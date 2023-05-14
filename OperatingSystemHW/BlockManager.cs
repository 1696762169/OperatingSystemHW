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
        private readonly List<(int start, int end)> m_FreeBlocks = new();   // 空闲盘块链表
        private readonly bool[] m_BlockUsed = new bool[DiskManager.TOTAL_SECTOR]; // 盘块使用情况表

        private readonly IDiskManager m_DiskManager;                        // 磁盘管理器

        public BlockManager(IDiskManager diskManager)
        {
            m_DiskManager = diskManager;
        }

        #region 公共接口
        public Block GetFreeBlock()
        {
            throw new NotImplementedException();
        }

        public Block GetBlock(int blockNo)
        {
            throw new NotImplementedException();
        }

        public void PutBlock(int blockNo)
        {
            throw new NotImplementedException();
        }

        public void ReadBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0)
        {
            throw new NotImplementedException();
        }

        public void WriteBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0)
        {
            throw new NotImplementedException();
        }
        #endregion

        // 读取一个外存Inode
        private DiskInode ReaDiskInode(int inodeNo)
        {
            const int START = DiskManager.INODE_START_SECTOR * DiskManager.SECTOR_SIZE;
            m_DiskManager.Read(START + inodeNo * Marshal.SizeOf<DiskInode>(), out DiskInode ret);
            return ret;
        }
    }
}
