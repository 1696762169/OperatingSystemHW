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
        public IUserManager UserManager { get; }

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
        public void CreateFile(string path);
        /// <summary>
        /// 删除文件
        /// </summary>
        public void DeleteFile(string path);

        /// <summary>
        /// 创建目录
        /// </summary>
        public void CreateDirectory(string path);
        /// <summary>
        /// 删除目录
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">目录或其子目录/文件已被占用</exception>
        public void DeleteDirectory(string path, bool deleteSub);

        /// <summary>
        /// 从文件读取二进制数据
        /// </summary>
        public void ReadBytes(OpenFile file, byte[] data);
        /// <summary>
        /// 写入二进制数据到文件
        /// </summary>
        public void WriteBytes(OpenFile file, byte[] data);
        /// <summary>
        /// 从文件读取结构体
        /// </summary>
        public void ReadStruct<T>(OpenFile file, out T value) where T : unmanaged;
        /// <summary>
        /// 写入结构体到文件
        /// </summary>
        public void WriteStruct<T>(OpenFile file, ref T value)where T : unmanaged;

        /// <summary>
        /// 移动文件指针
        /// </summary>
        public void Seek(OpenFile file, int pos, SeekType type = SeekType.Begin);

        /// <summary>
        /// 获取当前用户目录下的文件和目录
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Entry> GetEntries();

        /// <summary>
        /// 判断一个路径是否为一个已存在的文件
        /// </summary>
        public bool FileExists(string path);
        /// <summary>
        /// 判断一个路径是否为一个已存在的目录
        /// </summary>
        public bool DirectoryExists(string path);

        /// <summary>
        /// 切换当前工作目录
        /// </summary>
        public void ChangeDirectory(string path);
        /// <summary>
        /// 获取当前用户的工作目录
        /// </summary>
        public string GetCurrentPath();
    }
}
