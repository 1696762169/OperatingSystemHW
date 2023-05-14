using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    internal enum SeekType
    {
        Begin,
        Current,
        End
    }

    internal interface IFileManager
    {
        /// <summary>
        /// 打开文件
        /// </summary>
        public OpenFile Open(string path);
        /// <summary>
        /// 关闭文件
        /// </summary>
        public void Close(OpenFile file);

        /// <summary>
        /// 创建文件
        /// </summary>
        public void Create(string path);
        /// <summary>
        /// 删除文件
        /// </summary>
        public void Delete(string path);

        /// <summary>
        /// 读取文件
        /// </summary>
        public void Read(OpenFile file, byte[] buffer, int size);
        /// <summary>
        /// 写入文件
        /// </summary>
        public void Write(OpenFile file, byte[] buffer, int size);

        /// <summary>
        /// 移动文件指针
        /// </summary>
        public void Seek(OpenFile file, int pos, SeekType type = SeekType.Begin);
    }
}
