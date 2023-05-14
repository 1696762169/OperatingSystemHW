using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 打开文件结构表
    /// </summary>
    internal class OpenFileTable
    {
        public const int MAX_OPEN_FILE = 100;   // 最大打开文件数
        private List<OpenFile> m_OpenFiles = new(MAX_OPEN_FILE);    // 打开文件表

        /// <summary>
        /// 打开文件 添加一个打开文件结构
        /// </summary>
        /// <returns></returns>
        public OpenFile OpenFile()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 关闭文件 释放打开文件结构
        /// </summary>
        public void CloseFile()
        {

        }
    }
}
