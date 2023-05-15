using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 外存Inode结构
    /// </summary>
    internal struct DiskInode
    {
        public const int SIZE = 64; // 外存Inode结构大小

        public int mode;            // 状态的标志位
        public int linkCount;       // 文件联结计数，即该文件在目录树中不同路径名的数量

        public short uid;           // 文件所有者的用户标识数 0表示未使用
        public short gid;           // 文件所有者的组标识数

        public int size;            // 文件大小，字节为单位
        public unsafe fixed int address[10];    // 用于文件逻辑块和物理块转换的基本索引表

        // 这两个时间其实根本没有被使用
        public int dummyAccessTime;     // 最后访问时间
        public int dummyModifyTime;		// 最后修改时间

        /// <summary>
        /// 获取一个空Inode
        /// </summary>
        public static DiskInode Empty => new()
        {
            mode = 0,
            linkCount = 0,
            uid = 0,
            gid = 0,
            size = 0,
            dummyAccessTime = 0,
            dummyModifyTime = 0,
        };
    }
}
