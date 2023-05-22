using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// Inode状态标志
    /// </summary>
    internal enum InodeFlag
    {
        Lock = 0x1,     // 索引节点上锁
        Update = 0x2,   // 内存inode被修改过，需要更新相应外存inode
        Access = 0x4,   // 内存inode被访问过，需要修改最近一次访问时间
        Text = 0x8		// 内存inode对应进程图像的正文段
    }
    /// <summary>
    /// 内存Inode结构
    /// </summary>
    internal class Inode : IDisposable
    {
        //public InodeFlag flag;      // 状态标志
        //public int refCount;        // 引用计数

        public int mode;            // 状态的标志位
        public int linkCount;       // 文件联结计数，即该文件在目录树中不同路径名的数量

        public short uid;           // 文件所有者的用户标识数
        public short gid;           // 文件所有者的组标识数

        public int size;            // 文件大小，字节为单位
        public int[] address = new int[10];      // 用于文件逻辑块号和物理块号转换的基本索引表

        public readonly int number;  // Inode序号
        private readonly IInodeManager m_InodeManager;  // 用于释放资源的InodeManager
        public Inode(DiskInode diskInode, int number, IInodeManager inodeManager)
        {

            mode = diskInode.mode;
            linkCount = diskInode.linkCount;

            uid = diskInode.uid;
            gid = diskInode.gid;
            
            size = diskInode.size;
            unsafe
            {
                Marshal.Copy((IntPtr)diskInode.address, address, 0, 10);
            }

            this.number = number;
            m_InodeManager = inodeManager;
        }

        /// <summary>
        /// 将内存Inode转换为外存Inode
        /// </summary>
        public DiskInode ToDiskInode()
        {
            DiskInode diskInode = new()
            {
                mode = this.mode,
                linkCount = this.linkCount,
                uid = this.uid,
                gid = this.gid,
                size = this.size,
                dummyAccessTime = Utility.Time,
                dummyModifyTime = Utility.Time,
            };
            unsafe
            {
                Marshal.Copy(address, 0, (IntPtr)diskInode.address, 10);
            }
            return diskInode;
        }

        /// <summary>
        /// 使用BufferManager根据Inode地址项获取其使用的所有扇区
        /// </summary>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public IEnumerable<(int sectorNo, bool content)> GetUsedSectors(ISectorManager sectorManager, int start = 0, int length = -1, bool getAddress = true, bool getContent = true)
        {
            return Utility.GetUsedSectors(address, size, (sectorNo) =>
            {
                int[] ret = new int[DiskManager.SECTOR_SIZE / sizeof(int)];
                using Sector sector = sectorManager.GetSector(sectorNo);
                sectorManager.ReadArray(sector, ret, 0, ret.Length);
                return ret;
            }, start, length, getAddress, getContent);
        }
        /// <summary>
        /// 使用BufferManager 根据Inode地址项 获取其使用的所有实际内容扇区
        /// </summary>
        public IEnumerable<int> GetUsedContentSectors(ISectorManager sectorManager, int start = 0, int length = -1)
        {
            return GetUsedSectors(sectorManager, start, length, getAddress: false).Select(x => x.sectorNo);
        }
        /// <summary>
        /// 使用BufferManager 根据Inode地址项 获取其使用的所有索引扇区
        /// </summary>
        public IEnumerable<int> GetUsedAddressSectors(ISectorManager sectorManager, int start = 0, int length = -1)
        {
            return GetUsedSectors(sectorManager, start, length, getContent: false).Select(x => x.sectorNo);
        }
        /// <summary>
        /// 使用BufferManager 根据Inode地址项 获取指定字节数位置的一个扇区
        /// </summary>
        public int GetUsedSector(int start, ISectorManager sectorManager)
        {
            try
            {
                return GetUsedContentSectors(sectorManager, start, 1).First();
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException($"未找到字节 {start} 处的盘块", e);
            }
        }

        /// <summary>
        /// 申请写入文件所需的所有新扇区权限并返回
        /// </summary>
        /// <param name="prevSize">原文件大小 单位：字节</param>
        /// <param name="curSize">新文件大小 单位：字节</param>
        /// <param name="sectorManager">需要使用的扇区管理器</param>
        public List<Sector> GetWritingSectors(int curSize, int prevSize, ISectorManager sectorManager)
        {
            // 计算需要的所有新扇区数量与内容新扇区数量
            int sectorCount = DiskManager.GetSectorCount(curSize) - DiskManager.GetSectorCount(prevSize);
            // 获取所有新扇区的写入权限 保证可完成写入
            List<Sector> sectors = new(sectorCount);
            try
            {
                for (int i = 0; i < sectorCount; i++)
                    sectors.Add(new Sector(sectorManager.GetEmptySector(), sectorManager));
            }
            catch
            {
                // 未能找到所需的盘块时 释放所有已获得的资源
                while (sectors.Count > 0)
                {
                    sectorManager.PutSector(sectors.Last());
                    sectors.RemoveAt(sectors.Count - 1);
                }
                throw;
            }
            // 返回扇区
            return sectors;
        }

        /// <summary>
        /// 更新Inode的扇区索引数组 并更新间接索引扇区内容
        /// </summary>
        /// <param name="prevSize">原文件大小 单位：字节</param>
        /// <param name="curSize">新文件大小 单位：字节</param>
        /// <param name="sectorManager">需要使用的扇区管理器</param>
        /// <param name="newSectors">已经申请到使用权限的新扇区</param>
        /// <param name="addressSectors">已经申请到使用权限的原索引扇区</param>
        /// <param name="contentSectors">已经申请到使用权限的待写入内容扇区</param>
        /// <returns>按序排列的内容扇区</returns>
        public List<Sector> UpdateAddress(int curSize, int prevSize, ISectorManager sectorManager, 
            List<Sector> newSectors, List<Sector> addressSectors, List<Sector> contentSectors)
        {
            // 检查扇区数量
            int newSectorCount = DiskManager.GetSectorCount(curSize) - DiskManager.GetSectorCount(prevSize);
            if (newSectors.Count != newSectorCount)
                throw new ArgumentException($"新扇区数量不匹配，需要 {newSectorCount} 个，实际提供 {newSectors.Count} 个");
            int addressSectorCount = DiskManager.GetAddressSectorCount(prevSize);
            if (addressSectors.Count != addressSectorCount)
                throw new ArgumentException($"原索引扇区数量不匹配，需要 {addressSectorCount} 个，实际提供 {addressSectors.Count} 个");
            int contentSectorCount = DiskManager.GetContentSectorCount(prevSize);
            if (contentSectors.Count != contentSectorCount)
                throw new ArgumentException($"原内容扇区数量不匹配，需要 {contentSectorCount} 个，实际提供 {contentSectors.Count} 个");

            // 获取所有可能发生变化的索引盘块权限 保证后续更改成功
            //List<Sector> addressSectors = new();
            

            // 将扇区分为内容盘块和索引盘块两部分
            //List<Sector> contentSectors = new(newSectors.GetRange(0, contentSectorCount));
            //List<Sector> addressSectors = new(newSectors.GetRange(contentSectorCount, sectorCount - contentSectorCount));

            // 
            return contentSectors;
        }

        public void Dispose()
        {
            m_InodeManager.PutInode(number);
        }
    }
}
