using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件块管理器
    /// </summary>
    internal class BlockManager : IBlockManager
    {
        private readonly bool[] m_BlockUsed = new bool[DiskManager.TOTAL_SECTOR];   // 盘块使用情况表
        private int m_SearchFreeBlock = DiskManager.DATA_START_SECTOR;  // 空闲盘块搜索指针
        private int m_FreeBlockCount; // 空闲盘块数量

        private readonly IDiskManager m_DiskManager;    // 磁盘管理器
        private readonly ISuperBlockManager m_SuperBlockManager;    // 超级块管理器

        public BlockManager(IDiskManager diskManager, ISuperBlockManager superBlockManager)
        {
            m_DiskManager = diskManager;
            m_SuperBlockManager = superBlockManager;

            // 如有必要 格式化硬盘
            if (m_SuperBlockManager.Sb.GetSignature() != "Made by JYX")
                FormatDisk();

            // 设置已使用块（超级块必定被使用）
            for (int i = DiskManager.SUPER_BLOCK_SECTOR; i < DiskManager.SUPER_BLOCK_SECTOR + DiskManager.SUPER_BLOCK_SIZE; ++i)
                m_BlockUsed[i] = true;
            SetUsedBlocks(DiskManager.ROOT_INODE_NO, true);

            // 记录空闲盘块数量
            m_FreeBlockCount = m_SuperBlockManager.Sb.FreeCount;
        }

        #region 公共接口
        public IEnumerable<Block> GetFreeBlock(int count)
        {
            for (int i = 0; i < count; ++i)
                yield return GetFreeBlock();
        }

        public Block GetFreeBlock()
        {
            if (m_FreeBlockCount <= 0)
                throw new Exception("磁盘已满");
            while (m_BlockUsed[m_SearchFreeBlock])
            {
                ++m_SearchFreeBlock;
                if (m_SearchFreeBlock >= m_BlockUsed.Length)
                    m_SearchFreeBlock = DiskManager.DATA_START_SECTOR;
            }
            return GetBlock(m_SearchFreeBlock);
        }

        public Block GetBlock(int blockNo)
        {
            if (m_BlockUsed[blockNo])
                throw new Exception($"盘块 {blockNo} 已被使用");
            m_BlockUsed[blockNo] = true;
            if (blockNo >= DiskManager.DATA_START_SECTOR)
                --m_FreeBlockCount;
            return new Block(blockNo);
        }

        public void PutBlock(Block block)
        {
            m_BlockUsed[block.Number] = false;
            if (block.Number >= DiskManager.DATA_START_SECTOR)
                ++m_FreeBlockCount;
        }

        public void ReadBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0)
        {
            throw new NotImplementedException();
        }

        public void WriteBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0)
        {
            throw new NotImplementedException();
        }

        public void ReadStruct<T>(Block block, out T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void WriteStruct<T>(Block block, ref T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void ReadArray<T>(Block block, T[] array, int offset, int count) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void WriteArray<T>(Block block, T[] array, int offset, int count) where T : unmanaged
        {
            throw new NotImplementedException();
        }
        #endregion

        /// <summary>
        /// 递归读取外存Inode并设置已使用块
        /// </summary>
        /// <param name="inodeNo">当前设置外存Inode序号</param>
        /// <param name="dir">是否为目录文件</param>
        private void SetUsedBlocks(int inodeNo, bool dir)
        {
            // 读取外存Inode
            ReadDiskInode(inodeNo, out DiskInode inode);

            // 将其使用块设置为已使用
            m_BlockUsed[DiskManager.INODE_START_SECTOR + inodeNo / DiskManager.INODE_PER_SECTOR] = true;
            int[] address = new int[10];
            unsafe
            {
                Marshal.Copy((IntPtr)inode.address, address, 0, address.Length);
            }
            List<(int blockNo, bool content)> blocks = new(Utility.GetUsedBlocks(address, inode.size, (blockNo) =>
            {
                int[] buffer = new int[DiskManager.SECTOR_SIZE / sizeof(int)];
                m_DiskManager.ReadArray(blockNo * DiskManager.SECTOR_SIZE, buffer, 0, buffer.Length);
                return buffer;
            }));
            foreach (var tuple in blocks)
                m_BlockUsed[tuple.blockNo] = true;

            // 如果是目录文件 解析其所有目录项 并递归设置其子目录
            if (!dir)
                return;
            int dirCount = inode.size / Marshal.SizeOf<DirectoryEntry>();
            foreach (var (blockNo, content) in blocks)
            {
                if (!content)
                    continue;
                DirectoryEntry[] buffer = new DirectoryEntry[dirCount];
                m_DiskManager.ReadArray(blockNo * DiskManager.SECTOR_SIZE, buffer, 0, buffer.Length);
                // 设置子目录或文件
                foreach (var entry in buffer)
                {
                    byte[] nameBuffer = new byte[DirectoryEntry.NAME_MAX_COUNT];
                    unsafe
                    {
                        Marshal.Copy((IntPtr)entry.name, nameBuffer, 0, nameBuffer.Length);
                    }
                    string name = Utility.DecodeString(nameBuffer);
                    SetUsedBlocks(entry.inodeNo, !string.IsNullOrEmpty(name) && (name[^1] == '/' || name[^1] == '\\'));
                }
            }
        }

        // 读取一个外存Inode
        private void ReadDiskInode(int inodeNo, out DiskInode diskInode)
        {
            const int START = DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE;
            m_DiskManager.Read(START + inodeNo * Marshal.SizeOf<DiskInode>(), out diskInode);
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
            inode.accessTime = inode.modifyTime = Utility.Time;
            m_DiskManager.Write(DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE + DiskManager.ROOT_INODE_NO * Marshal.SizeOf<DiskInode>(), ref inode);
        }
    }
}
