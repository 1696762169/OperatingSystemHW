using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.test
{
    internal class FileReadWrite : IDisposable, IDiskManager
    {
        private readonly FileStream m_File;

        public FileReadWrite(string filePath)
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
                m_File = File.Create(filePath);
                m_File.SetLength(DiskManager.TOTAL_SECTOR * DiskManager.SECTOR_SIZE);
            }
            else
            {
                m_File = File.Open(filePath, FileMode.Open);
            }
        }

        public void ReadBytes(byte[] buffer, int offset, int count)
        {
            m_File.Seek(offset, SeekOrigin.Begin);
            _ = m_File.Read(buffer, 0, count);
        }

        public void WriteBytes(byte[] buffer, int offset)
        {
            m_File.Seek(offset, SeekOrigin.Begin);
            m_File.Write(buffer, 0, buffer.Length);
        }

        public void Read<T>(int position, out T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Write<T>(int position, ref T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void ReadArray<T>(int position, T[] array, int offset, int count) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void WriteArray<T>(int position, T[] array, int offset, int count) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            m_File.Dispose();
        }
    }
}
