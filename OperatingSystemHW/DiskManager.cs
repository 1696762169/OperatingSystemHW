using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable CA1416

namespace OperatingSystemHW
{
    /// <summary>
    /// img文件操作类
    /// </summary>
    internal class DiskManager : IDisposable, IDiskManager
    {
        #region 常量定义
        public const int SUPER_BLOCK_SECTOR = 0;    // 定义SuperBlock位于磁盘上的扇区号 占据0、1两个扇区
        public const int SUPER_BLOCK_SIZE = 2;  // 定义SuperBlock占据的扇区数

        public const int ROOT_INODE_NO = 1; // 文件系统根目录外存Inode编号
        public const int INODE_PER_SECTOR = SECTOR_SIZE / DiskInode.SIZE;    // 每个磁盘块可以存放的外存Inode数
        public const int INODE_START_SECTOR = SUPER_BLOCK_SECTOR + SUPER_BLOCK_SIZE;   // 外存Inode区位于磁盘上的起始扇区号
        public const int INODE_SIZE = DATA_START_SECTOR - INODE_START_SECTOR;   // 外存Inode区占用的盘块数

        public const int DATA_START_SECTOR = 1024;      // 数据区的起始扇区号
        public const int DATA_SIZE = 1 << 14;   // 数据区占据的扇区数量（16384）

        public const int SECTOR_SIZE = 512; // 扇区大小
        public const int TOTAL_SECTOR = DATA_SIZE + DATA_START_SECTOR;   // 总扇区数

        public const int INT_PER_SECTOR = SECTOR_SIZE / sizeof(int);
        public const int SMALL_FILE_SIZE = SECTOR_SIZE * 6;  // 小文件最大字节数
        public const int LARGE_FILE_SIZE = SECTOR_SIZE * (6 + INT_PER_SECTOR * 2);   // 大文件最大字节数
        public const int HUGE_FILE_SIZE = SECTOR_SIZE * (6 + INT_PER_SECTOR * (2 + INT_PER_SECTOR * 2)); // 巨大文件最大字节数

        private const string MEMORY_NAME = "MadeByJYX";
        #endregion

        private readonly MemoryMappedFile m_MappedFile;    // 内存映射文件

        public readonly MemoryMappedViewAccessor accessor;   // 内存映射视图访问器 用于外界读写文件

        public DiskManager(string filePath, bool forceCreate = false)
        {
            // 当img文件不存在时创建该文件
            bool isFileExist = File.Exists(filePath);
            if (!isFileExist || forceCreate)
            {
                // 创建目录
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                // 创建文件并设定大小
                using FileStream fs = File.Create(filePath);
                fs.SetLength(TOTAL_SECTOR * SECTOR_SIZE);
            }

            // 尝试打开已经存在的内存映射文件
            try
            {
                m_MappedFile = MemoryMappedFile.OpenExisting(MEMORY_NAME);
                Console.WriteLine("打开了已有内存映射文件");
            }
            catch (FileNotFoundException)
            {
                // 创建内存映射文件
                m_MappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, MEMORY_NAME, TOTAL_SECTOR * SECTOR_SIZE);
            }
            
            // 创建内存映射视图访问器
            accessor = m_MappedFile.CreateViewAccessor();

            // 强制清除签名 通知BlockManager格式化硬盘
            if (!isFileExist || forceCreate)
            {
                byte[] empty = new byte[SUPER_BLOCK_SIZE * SECTOR_SIZE];
                WriteBytes(empty, SUPER_BLOCK_SECTOR * SECTOR_SIZE);
            }
        }

        public void Dispose()
        {
            accessor.Dispose();
            m_MappedFile.Dispose();
        }

        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            accessor.ReadArray(offset, buffer, 0, count);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            accessor.WriteArray(offset, buffer, 0, buffer.Length);
        }

        public void WriteBytes(byte[] buffer, int offset, int count)
        {
            accessor.WriteArray(offset, buffer, 0, count);
        }

        public void Read<T>(int position, out T value) where T : unmanaged
        {
            accessor.Read(position, out value);
        }

        public void Write<T>(int position, ref T value) where T : unmanaged
        {
            accessor.Write(position, ref value);
        }

        public void ReadArray<T>(int position, T[] array, int offset, int count) where T : unmanaged
        {
            accessor.ReadArray(position, array, offset, count);
        }

        public void WriteArray<T>(int position, T[] array, int offset, int count) where T : unmanaged
        {
            accessor.WriteArray(position, array, offset, count);
        }

        /// <summary>
        /// 获取一个文件需要使用的所有扇区数量
        /// </summary>
        /// <param name="size">文件大小 单位：字节</param>
        public static int GetAddressSectorCount(int size)
        {
            switch (size)
            {
            case < 0:
                throw new ArgumentException("文件大小不能为负数");
            case > HUGE_FILE_SIZE:
                throw new ArgumentException($"文件（{size}字节）过大");
            }
            // 分段计算索引盘块数
            const int PAGE_SIZE = INT_PER_SECTOR * SECTOR_SIZE;
            switch (size)
            {
            case <= SMALL_FILE_SIZE:
                return 0;
            case <= LARGE_FILE_SIZE:
                size -= SMALL_FILE_SIZE;
                return size >= PAGE_SIZE ? 2 : 1;
            default:
                size -= LARGE_FILE_SIZE;
                return 2 + (size >= INT_PER_SECTOR * PAGE_SIZE ? 2 : 1) + (size + PAGE_SIZE - 1) / PAGE_SIZE;
            }
        }
        /// <summary>
        /// 获取一个文件需要使用的内容扇区数量
        /// </summary>
        /// <param name="size">文件大小 单位：字节</param>
        public static int GetContentSectorCount(int size) => (size + SECTOR_SIZE - 1) / SECTOR_SIZE;
        /// <summary>
        /// 获取一个文件需要使用的内容扇区数量
        /// </summary>
        /// <param name="size">文件大小 单位：字节</param>
        public static int GetSectorCount(int size) => GetAddressSectorCount(size) + GetContentSectorCount(size);
    }
}
