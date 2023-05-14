using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// Inode存储表
    /// </summary>
    internal class InodeTable
    {
        public const int MAX_INODE = 100;   // 最大Inode数
        private List<Inode> m_Inodes = new(MAX_INODE);    // Inode表
        private readonly FileSystem m_FileSystem;   // 引用的FileSystem

        public InodeTable(FileSystem fileSystem)
        {
            m_FileSystem = fileSystem;
        }


        /// <summary>
        /// 申请一个Inode
        /// </summary>
        public Inode AllocInode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 释放一个Inode
        /// </summary>
        /// <param name="inode"></param>
        public void FreeInode(Inode inode)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 将所有被修改过的内存Inode更新到对应的外存Inode中
        /// </summary>
        public void FlushInode()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取某一编号的Inode在内存中的副本序号
        /// </summary>
        /// <returns>-1表示没有被加载到内存中</returns>
        public bool GetLoadedIndex(int number)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 获取一个在内存中的空闲Inode
        /// </summary>
        /// <returns>null表示没有空闲Inode</returns>
        public Inode GetFreeInode()
        {
            throw new NotImplementedException();
        }
    }
}
