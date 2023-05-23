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
        /// 更新Inode的扇区索引数组 并更新间接索引扇区内容
        /// </summary>
        /// <param name="prevSize">原文件大小 单位：字节</param>
        /// <param name="newSize">新文件大小 单位：字节</param>
        /// <param name="sectorManager">需要使用的扇区管理器</param>
        /// <param name="newSectors">已经申请到使用权限的新扇区</param>
        /// <param name="addressSectors">已经申请到使用权限的所有原索引扇区</param>
        /// <returns>按序排列的内容扇区</returns>
        public List<Sector> UpdateAddress(int newSize, int prevSize, ISectorManager sectorManager, List<Sector> newSectors, List<Sector> addressSectors)
        {
            // 检查扇区数量
            int newCount = DiskManager.GetSectorCount(newSize) - DiskManager.GetSectorCount(prevSize);
            if (newSectors.Count != newCount)
                throw new ArgumentException($"新扇区数量不匹配，需要 {newCount} 个，实际提供 {newSectors.Count} 个");
            int addressCount = DiskManager.GetAddressSectorCount(prevSize);
            if (addressSectors.Count != addressCount)
                throw new ArgumentException($"原索引扇区数量不匹配，需要 {addressCount} 个，实际提供 {addressSectors.Count} 个");

            // 将新扇区分为内容盘块和索引盘块两部分
            int newContentCount = DiskManager.GetContentSectorCount(newSize) - DiskManager.GetContentSectorCount(prevSize);
            int newAddressCount = newCount - newContentCount;
            List<Sector> newContentSectors = new(newSectors.GetRange(0, newContentCount)); 
            addressSectors.AddRange(newSectors.GetRange(newContentCount, newAddressCount));

            // 更新索引扇区内容
            UpdateAddressImpl(newSize, prevSize, sectorManager, newContentSectors, addressSectors);

            // 释放索引扇区
            addressSectors.ForEach(sector => sector.Dispose());
            // 返回新增的待写入的内容扇区
            return newContentSectors;
        }

        // 实际更新索引扇区的函数
        private void UpdateAddressImpl(int newSize, int prevSize, ISectorManager sectorManager, List<Sector> newContentSectors, List<Sector> addressSectors)
        {
            int start = DiskManager.GetContentSectorCount(prevSize);    // 使用的最小扇区号
            int end = DiskManager.GetContentSectorCount(newSize) - 1;   // 使用的最大扇区号

            // 一级索引无需设置索引扇区
            int index = 0;
            int sectorIndex = 0;
            int newIndex = 0;
            for (; index < 6 && sectorIndex <= end; ++index, ++sectorIndex)
                if (sectorIndex >= start)   // 只更新需要更新的扇区
                    address[index] = newContentSectors[newIndex++].number;
            if (sectorIndex > end)
                return;

            // 二级索引
            int[] buffer = new int[DiskManager.INT_PER_SECTOR];
            for (; index < 8 && sectorIndex <= end; ++index)
            {
                // 获取二级索引扇区
                Sector curSector = addressSectors[index - 6];
                sectorManager.ReadArray(curSector, buffer, 0, DiskManager.INT_PER_SECTOR);
                for (int j = 0; j < buffer.Length && sectorIndex <= end; ++j, ++sectorIndex)
                    if (sectorIndex >= start)   // 只更新需要更新的扇区
                        buffer[j] = newContentSectors[newIndex++].number;
                // 写回扇区数据
                sectorManager.WriteArray(curSector, buffer, 0, DiskManager.INT_PER_SECTOR);
                // 写入地址
                address[index] = curSector.number;
            }
            if (sectorIndex > end)
                return;

            // 三级索引
            int[] buffer2 = new int[DiskManager.INT_PER_SECTOR];
            for (; index < 10 && sectorIndex <= end; ++index)
            {
                // 获取三级索引扇区
                Sector curSector = addressSectors[2 + (index - 8) * (1 + DiskManager.INT_PER_SECTOR)];
                sectorManager.ReadArray(curSector, buffer, 0, DiskManager.INT_PER_SECTOR);
                for (int page = 0; page < buffer.Length && sectorIndex <= end; ++page)
                {
                    // 获取二级索引扇区
                    Sector curSector2 = addressSectors[2 + (index - 8) * (1 + DiskManager.INT_PER_SECTOR) + 1 + page];
                    sectorManager.ReadArray(curSector2, buffer2, 0, DiskManager.INT_PER_SECTOR);
                    for (int k = 0; k < buffer2.Length && sectorIndex <= end; ++k, ++sectorIndex)
                        if (sectorIndex >= start)   // 只更新需要更新的扇区
                            buffer2[k] = newContentSectors[newIndex++].number;
                    // 写回二级索引扇区数据
                    sectorManager.WriteArray(curSector2, buffer2, 0, DiskManager.INT_PER_SECTOR);
                    // 更新三级索引
                    buffer[page] = curSector2.number;
                }
                // 写回三级索引扇区数据
                sectorManager.WriteArray(curSector, buffer, 0, DiskManager.INT_PER_SECTOR);
                // 写入地址
                address[index] = curSector.number;
            }
        }

        public void Clear()
        {
            uid = 0;
            size = 0;
            address = new int[10];
        }

        public void Dispose()
        {
            m_InodeManager.PutInode(number);
        }
    }
}
