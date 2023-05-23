#define DEBUG_CHECK_FREE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件块管理器
    /// </summary>
    internal class BlockManager : ISectorManager, IInodeManager
    {
        private readonly bool[] m_SectorUsed = new bool[DiskManager.TOTAL_SECTOR];   // 扇区使用情况表
        private int m_SearchFreeSector = DiskManager.DATA_START_SECTOR;  // 空闲扇区搜索指针
        private int FreeDataSector
        {
            get => m_SuperBlockManager.Sb.FreeCount;
            set => m_SuperBlockManager.Sb.SetFreeSector(value);
        } // 空闲扇区数量
        private readonly HashSet<int> m_SectorLocks = new(); // 扇区被进程占用情况记录

        private readonly bool[] m_InodeUsed = new bool[DiskManager.INODE_SIZE * DiskManager.INODE_PER_SECTOR]; // Inode使用情况表
        private int m_SearchFreeInode = 1;  // 空闲Inode搜索指针（不可尝试搜索0号Inode）
        private int FreeInode
        {
            get => m_SuperBlockManager.Sb.FreeInode;
            set => m_SuperBlockManager.Sb.SetFreeInode(value);
        } // 空闲Inode数量
        private readonly HashSet<int> m_InodeLocks = new(); // Inode被进程占用情况记录


        private readonly IDiskManager m_DiskManager;    // 磁盘管理器
        private readonly ISuperBlockManager m_SuperBlockManager;    // 超级块管理器

        public BlockManager(IDiskManager diskManager, ISuperBlockManager superBlockManager)
        {
            m_DiskManager = diskManager;
            m_SuperBlockManager = superBlockManager;

            // 如有必要 格式化硬盘
            if (m_SuperBlockManager.Sb.GetSignature() != "Made by JYX")
                FormatDisk();

            // 设置已使用块（超级块必定被使用）与Inode
            for (int i = DiskManager.SUPER_BLOCK_SECTOR; i < DiskManager.SUPER_BLOCK_SECTOR + DiskManager.SUPER_BLOCK_SIZE; ++i)
                m_SectorUsed[i] = true;
            SetUsedSectors(DiskManager.ROOT_INODE_NO, true);

#if DEBUG_CHECK_FREE
            // 检查空闲扇区数量和空闲Inode数量是否正确
            int freeSector = 0;
            for (int i = DiskManager.DATA_START_SECTOR; i < m_SectorUsed.Length; ++i)
                if (!m_SectorUsed[i])
                    ++freeSector;
            int freeInode = m_InodeUsed.Count(t => !t);
            if (freeSector != FreeDataSector)
            {
                FreeDataSector = freeSector;
                m_SuperBlockManager.UpdateSuperBlock();
                Console.WriteLine($"空闲扇区数量不正确，已修正为{freeSector}");
            }
            else
            {
                Console.WriteLine($"空闲扇区数量正确，为{freeSector}");
            }
            if (freeInode != FreeInode)
            {
                FreeInode = freeInode;
                m_SuperBlockManager.UpdateSuperBlock();
                Console.WriteLine($"空闲Inode数量不正确，已修正为{freeInode}");
            }
            else
            {
                Console.WriteLine($"空闲Inode数量正确，为{freeInode}");
            }
#endif
        }

        #region 数据操作公共接口
        public int GetEmptySector()
        {
            if (FreeDataSector <= 0)
                throw new Exception("磁盘已满");
            // 查找一个磁盘中未使用 且 未被其它进程占用的扇区
            while (m_SectorUsed[m_SearchFreeSector] || m_SectorLocks.Contains(m_SearchFreeSector))
            {
                ++m_SearchFreeSector;
                if (m_SearchFreeSector >= m_SectorUsed.Length)
                    m_SearchFreeSector = DiskManager.DATA_START_SECTOR;
            }
            return m_SearchFreeSector++;
        }

        public Sector GetSector(int sectorNo)
        {
            if (m_SectorLocks.Contains(sectorNo))
                throw new UnauthorizedAccessException($"扇区 {sectorNo} 已被使用");
            m_SectorLocks.Add(sectorNo);
            return new Sector(sectorNo, this);
        }

        public void PutSector(Sector sector)
        {
            m_SectorLocks.Remove(sector.number);
        }

        public void ReadBytes(Sector sector, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int position = 0)
        {
            EnsureSectorUsed(sector);
            if (size + position > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException($"读取范围（position：{position} + size：{size}）超过扇区大小");
            m_DiskManager.ReadBytes(buffer, DiskManager.SECTOR_SIZE * sector.number + position, size);
        }

        public void WriteBytes(Sector sector, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int position = 0)
        {
            EnsureSectorUsed(sector);
            if (position + size > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException($"写入范围（position：{position} + size：{size}）超过扇区大小");
            OccupySector(sector.number);
            m_DiskManager.WriteBytes(buffer, DiskManager.SECTOR_SIZE * sector.number + position, size);
        }

        public void ReadStruct<T>(Sector sector, out T value, int position = 0) where T : unmanaged
        {
            EnsureSectorUsed(sector);
            if (position + Marshal.SizeOf<T>() > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException($"读取范围（position：{position} + sizeof({typeof(T).Name})：{Marshal.SizeOf<T>()}）超过扇区大小");
            m_DiskManager.Read(DiskManager.SECTOR_SIZE * sector.number + position, out value);
        }

        public void WriteStruct<T>(Sector sector, ref T value, int position = 0) where T : unmanaged
        {
            EnsureSectorUsed(sector);
            if (position + Marshal.SizeOf<T>() > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException($"写入范围（position：{position} + sizeof({typeof(T).Name})：{Marshal.SizeOf<T>()}）超过扇区大小");
            OccupySector(sector.number);
            m_DiskManager.Write(DiskManager.SECTOR_SIZE * sector.number + position, ref value);
        }

        public void ReadArray<T>(Sector sector, T[] array, int offset, int count, int position = 0) where T : unmanaged
        {
            EnsureSectorUsed(sector);
            if (position + count * Marshal.SizeOf<T>() > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException(
                    $"读取范围（position：{position} + count：{count} * sizeof({typeof(T).Name})：{Marshal.SizeOf<T>()}）超过扇区大小");
            m_DiskManager.ReadArray(DiskManager.SECTOR_SIZE * sector.number + position, array, offset, count);
        }

        public void WriteArray<T>(Sector sector, T[] array, int offset, int count, int position = 0) where T : unmanaged
        {
            EnsureSectorUsed(sector);
            if (position + count * Marshal.SizeOf<T>() > DiskManager.SECTOR_SIZE)
                throw new ArgumentOutOfRangeException(
                    $"写入范围（position：{position} + count：{count} * sizeof({typeof(T).Name})：{Marshal.SizeOf<T>()}）超过扇区大小");
            OccupySector(sector.number);
            m_DiskManager.WriteArray(DiskManager.SECTOR_SIZE * sector.number + position, array, offset, count);
        }

        public void ClearSector(params Sector[] sectors)
        {
            // 先检查使用权限
            foreach (Sector sector in sectors)
                EnsureSectorUsed(sector);
            // 释放所有扇区
            foreach (Sector sector in sectors)
                FreeSector(sector.number);
        }

        #endregion

        #region Inode操作公共接口
        public Inode GetEmptyInode()
        {
            if (FreeInode <= 0)
                throw new Exception("没有空闲的Inode");
            // 查找一个磁盘中未使用 且 未被其它进程占用的Inode
            while (m_InodeUsed[m_SearchFreeInode] || m_InodeLocks.Contains(m_SearchFreeInode))
            {
                ++m_SearchFreeInode;
                if (m_SearchFreeInode >= m_InodeUsed.Length)
                    m_SearchFreeInode = 1;
            }
            return GetInode(m_SearchFreeInode++);
        }

        public Inode GetInode(int inodeNo)
        {
            if (m_InodeLocks.Contains(inodeNo))
                throw new UnauthorizedAccessException($"Inode {inodeNo} 已被使用");
            m_InodeLocks.Add(inodeNo);

            ReadDiskInode(inodeNo, out DiskInode diskInode);
            return new Inode(diskInode, inodeNo, this);
        }

        public void PutInode(int inodeNo)
        {
            m_InodeLocks.Remove(inodeNo);
        }

        public void UpdateInode(int inodeNo, Inode inode)
        {
            // 检查Inode使用权限
            EnsureInodeUsed(inodeNo);
            // 清除uid为0的Inode占用
            if (inode.uid == 0 && m_InodeUsed[inodeNo])
            {
                ++FreeInode;
                m_InodeUsed[inodeNo] = false;
                m_SuperBlockManager.UpdateSuperBlock();
                return;
            }

            // 写入扇区内容
            if (!m_InodeUsed[inodeNo])
            {
                --FreeInode;
                m_InodeUsed[inodeNo] = true;
                m_SuperBlockManager.UpdateSuperBlock();
            }
            DiskInode diskInode = inode.ToDiskInode();
            WriteDiskInode(inodeNo, ref diskInode);
        }
        #endregion

        /// <summary>
        /// 递归读取外存Inode并设置已使用块
        /// </summary>
        /// <param name="inodeNo">当前设置外存Inode序号</param>
        /// <param name="dir">是否为目录文件</param>
        private void SetUsedSectors(int inodeNo, bool dir)
        {
            // 读取外存Inode
            ReadDiskInode(inodeNo, out DiskInode inode);
            // 记录该Inode已使用
            m_InodeUsed[inodeNo] = true;

            // 将其使用块设置为已使用
            m_SectorUsed[DiskManager.INODE_START_SECTOR + inodeNo / DiskManager.INODE_PER_SECTOR] = true;
            int[] address = new int[10];
            unsafe
            {
                Marshal.Copy((IntPtr)inode.address, address, 0, address.Length);
            }
            List<(int sectorNo, bool content)> sectors = new(Utility.GetUsedSectors(address, inode.size, (blockNo) =>
            {
                int[] buffer = new int[DiskManager.SECTOR_SIZE / sizeof(int)];
                m_DiskManager.ReadArray(blockNo * DiskManager.SECTOR_SIZE, buffer, 0, buffer.Length);
                return buffer;
            }));
            foreach (var tuple in sectors)
                m_SectorUsed[tuple.sectorNo] = true;

            // 如果是目录文件 解析其所有目录项 并递归设置其子目录
            if (!dir)
                return;
            int dirCount = inode.size / Marshal.SizeOf<DirectoryEntry>();
            DirectoryEntry[] buffer = new DirectoryEntry[DiskManager.SECTOR_SIZE / Marshal.SizeOf<DirectoryEntry>()];
            byte[] nameBuffer = new byte[DirectoryEntry.NAME_MAX_COUNT];
            foreach (var (sectorNo, content) in sectors)
            {
                if (!content)
                    continue;
                m_DiskManager.ReadArray(sectorNo * DiskManager.SECTOR_SIZE, buffer, 0, buffer.Length);
                // 设置子目录或文件
                foreach (var entry in buffer)
                {
                    unsafe
                    {
                        Marshal.Copy((IntPtr)entry.name, nameBuffer, 0, nameBuffer.Length);
                    }
                    string name = Utility.DecodeString(nameBuffer);
                    SetUsedSectors(entry.inodeNo, !string.IsNullOrEmpty(name) && PathUtility.IsDirectory(name));
                    if (--dirCount <= 0)
                        return;
                }
            }
        }

        // 读取一个外存Inode
        private void ReadDiskInode(int inodeNo, out DiskInode diskInode)
        {
            const int START = DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE;
            m_DiskManager.Read(START + inodeNo * DiskInode.SIZE, out diskInode);
        }
        // 写入一个外存Inode
        private void WriteDiskInode(int inodeNo, ref DiskInode diskInode)
        {
            const int START = DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE;
            m_DiskManager.Write(START + inodeNo * DiskInode.SIZE, ref diskInode);
        }

        // 判断一个扇区是否可用
        private void EnsureSectorUsed(Sector sector)
        {
            if (!m_SectorLocks.Contains(sector.number))
                throw new Exception($"扇区 {sector.number} 未被使用");
        }
        // 判断一个Inode是否可用
        private void EnsureInodeUsed(int inodeNo)
        {
            if (!m_InodeLocks.Contains(inodeNo))
                throw new Exception($"Inode {inodeNo} 未被使用");
        }

        // 占用扇区空间
        private void OccupySector(int sectorNo)
        {
            if (m_SectorUsed[sectorNo])
                return;
            m_SectorUsed[sectorNo] = true;
            --FreeDataSector;
            m_SuperBlockManager.UpdateSuperBlock();
        }
        // 释放扇区空间
        private void FreeSector(int sectorNo)
        {
            if (!m_SectorUsed[sectorNo])
                return;
            m_SectorUsed[sectorNo] = false;
            ++FreeDataSector;
            m_SuperBlockManager.UpdateSuperBlock();
        }

        // 格式化硬盘
        private void FormatDisk()
        {
            // 写入超级块签名
            m_SuperBlockManager.Sb.SetSignature("Made by JYX");
            m_SuperBlockManager.UpdateSuperBlock();

            // 格式化Inode区
            DiskInode inode = DiskInode.Empty;
            for (int i = 0; i < DiskManager.INODE_SIZE * DiskManager.INODE_PER_SECTOR; i++)
                m_DiskManager.Write(DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE + i * Marshal.SizeOf<DiskInode>(), ref inode);
            // 设置根目录Inode
            inode.linkCount = 1;
            inode.uid = DiskUser.SUPER_USER_ID;
            inode.gid = DiskUser.DEFAULT_GROUP_ID;
            inode.dummyAccessTime = inode.dummyModifyTime = Utility.Time;
            m_DiskManager.Write(DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE + DiskManager.ROOT_INODE_NO * Marshal.SizeOf<DiskInode>(), ref inode);
        }
    }
}
