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
        public int m_InodeIndex;    // 文件对应的外存Inode索引
        public int m_Count;         // 读写指针在文件中的位置
        public int m_Flags;         // 读写标志位
    }
}
