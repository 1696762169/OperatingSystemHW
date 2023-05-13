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
        public int mode;           // 状态的标志位，定义见enum INodeFlag
        public int linkCount;      // 文件联结计数，即该文件在目录树中不同路径名的数量

        public short uid;          // 文件所有者的用户标识数
        public short gid;          // 文件所有者的组标识数

        public int size;           // 文件大小，字节为单位
        public unsafe fixed int address[10];      // 用于文件逻辑块好和物理块好转换的基本索引表

        public int accessTime;     // 最后访问时间
        public int modifyTime;		// 最后修改时间
    }
}
