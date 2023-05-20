using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 打开文件结构
    /// </summary>
    internal class OpenFile
    {
        public readonly Inode inode;    // 文件对应的Inode
        public int pointer;             // 读写指针在文件中的位置

        public OpenFile(Inode inode)
        {
            this.inode = inode;
            pointer = 0;
        }
    }
}
