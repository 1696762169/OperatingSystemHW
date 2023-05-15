using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// img文件操作类
    /// </summary>
    internal class DiskManager : IDisposable, IDiskManager
    {
        #region 常量定义
        public const int SUPER_BLOCK_SECTOR = 0;    // 定义SuperBlock位于磁盘上的扇区号 占据0、1两个扇区

        public const int ROOT_INODE_NO = 1; // 文件系统根目录外存Inode编号
        public const int INODE_PER_SECTOR = SECTOR_SIZE / DiskInode.SIZE;    // 每个磁盘块可以存放的外存Inode数
        public const int INODE_START_SECTOR = SUPER_BLOCK_SECTOR + 2;   // 外存Inode区位于磁盘上的起始扇区号
        public const int INODE_SIZE = DATA_START_SECTOR - INODE_START_SECTOR;   // 外存Inode区占用的盘块数

        public const int DATA_START_SECTOR = 1024;      // 数据区的起始扇区号
        public const int DATA_SIZE = 1 << 14;   // 数据区占据的扇区数量（16384）

        public const int SECTOR_SIZE = 512; // 扇区大小
        public const int TOTAL_SECTOR = DATA_SIZE + DATA_START_SECTOR;   // 总扇区数
        #endregion

        private readonly MemoryMappedFile m_MappedFile;    // 内存映射文件

        public readonly MemoryMappedViewAccessor accessor;   // 内存映射视图访问器 用于外界读写文件

        public DiskManager(string filePath, bool forceCreate = false)
        {
            // 当img文件不存在时创建该文件
            bool isFileExist = File.Exists(filePath);
            if (!isFileExist)
            {
                // 创建目录
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                // 创建文件并设定大小
                using FileStream fs = File.Create(filePath);
                fs.SetLength(TOTAL_SECTOR * SECTOR_SIZE);
            }

            // 创建内存映射文件
            m_MappedFile = MemoryMappedFile.CreateFromFile(filePath);
            // 创建内存映射视图访问器
            accessor = m_MappedFile.CreateViewAccessor();

            // 格式化硬盘
            if (!isFileExist || forceCreate)
                CreateDisk();
        }

        public void Dispose()
        {
            m_MappedFile.Dispose();
            accessor.Dispose();
        }

        // 格式化硬盘
        private void CreateDisk()
        {
            // 写入超级块
            SuperBlock sb = SuperBlock.Init();
            accessor.Write(SUPER_BLOCK_SECTOR * SECTOR_SIZE, ref sb);

            // 初始化Inode区
            DiskInode inode = new()
            {
                mode = 0,
                linkCount = 0,
                uid = 0,
                gid = 0,
                size = 0,
                accessTime = Utility.Time,
                modifyTime = Utility.Time,
            };
            for (int i = 0; i < INODE_SIZE * INODE_PER_SECTOR; i++)
                accessor.Write(INODE_START_SECTOR * SECTOR_SIZE + i * Marshal.SizeOf<DiskInode>(), ref inode);
            inode.uid = DiskUser.SUPER_USER_ID;
            accessor.Write(INODE_START_SECTOR * SECTOR_SIZE + ROOT_INODE_NO * Marshal.SizeOf<DiskInode>(), ref inode);
        }

        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            accessor.ReadArray(offset, buffer, 0, count);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            accessor.WriteArray(offset, buffer, 0, buffer.Length);
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
    }
}
