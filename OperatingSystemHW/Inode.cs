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
        public int[] address = new int[10];      // 用于文件逻辑块好和物理块好转换的基本索引表

        private readonly int m_Index;  // Inode序号
        private readonly IInodeManager m_InodeManager;  // 用于释放资源的InodeManager
        public Inode(DiskInode diskInode, int index, IInodeManager inodeManager)
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

            m_Index = index;
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

        public void Dispose()
        {
            m_InodeManager.PutInode(m_Index);
        }
    }
}
