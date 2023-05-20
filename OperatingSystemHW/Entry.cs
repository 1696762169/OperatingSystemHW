using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件目录项
    /// </summary>
    internal struct DirectoryEntry
    {
        // 遵循原著 就定28吧
        public const int NAME_MAX_COUNT = 28;

        public int inodeNo;         // 文件Inode编号
        public unsafe fixed byte name[NAME_MAX_COUNT];         // 文件名
    }

    /// <summary>
    /// 内存中使用的文件目录项
    /// </summary>
    internal class Entry
    {
        public int inodeNo;
        public string name;

        public Entry(int inodeNo, string name)
        {
            this.inodeNo = inodeNo;
            this.name = name;
        }
        public Entry(DirectoryEntry entry)
        {
            inodeNo = entry.inodeNo;
            unsafe
            {
                name = Utility.DecodeString(entry.name, DirectoryEntry.NAME_MAX_COUNT);
            }
        }

        public DirectoryEntry ToDirectoryEntry()
        {
            DirectoryEntry entry = new()
            {
                inodeNo = this.inodeNo
            };
            unsafe
            {
                byte[] buffer = Utility.EncodeString(name, DirectoryEntry.NAME_MAX_COUNT);
                Marshal.Copy(buffer, 0, (IntPtr)entry.name, buffer.Length);
            }
            return entry;
        }
    }
}
