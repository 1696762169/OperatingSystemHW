using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OperatingSystemHW
{
    internal static class Utility
    {
        private static readonly DateTime _StartTime = new (1970, 1, 1);
        public static int Time => (int)DateTime.Now.Subtract(_StartTime).TotalSeconds;

        public static string ToTime(int time)
        {
            DateTime dt = _StartTime.AddSeconds(time);
            return dt.ToString("u");
        }

        public static byte[] EncodeString(string str) => Encoding.UTF8.GetBytes(str);
        public static byte[] EncodeString(string str, int size)
        {
            byte[] buffer = new byte[size];
            Array.Copy(EncodeString(str), buffer, Math.Min(size, str.Length));
            return buffer;
        }
        public static string DecodeString(byte[] data) => Encoding.UTF8.GetString(data).Trim('\0');
        public static unsafe string DecodeString(byte* data, int count) => Encoding.UTF8.GetString(data, count).Trim('\0');

        /// <summary>
        /// 根据Inode地址项获取其使用的所有盘块
        /// </summary>
        /// <param name="address">地址数组 必须为10位</param>
        /// <param name="size">文件大小 单位：字节</param>
        /// <param name="getSector">获取盘块的方法 一般需要使用BlockManager</param>
        /// <param name="start">获取内容量起始位置 单位：字节</param>
        /// <param name="length">获取内容量大小 单位：字节</param>
        /// <param name="getAddress">是否获取索引盘块</param>
        /// <param name="getContent">是否获取内容盘块</param>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public static IEnumerable<(int sectorNo, bool content)> GetUsedSectors(IReadOnlyList<int> address, int size, Func<int, int[]> getSector,
            int start = 0, int length = -1, bool getAddress = true, bool getContent = true)
        {
            // 获取长度默认为读取到文件结尾
            if (length < 0)
                length = size - start;

            int returnStart = start / DiskManager.SECTOR_SIZE;
            int returnEnd = DiskManager.GetContentSectorCount(Math.Min(start + length, size));
            int sector = 0;

            // 一级索引
            int index = 0;
            for (; index < 6 && sector < returnEnd; ++index, ++sector)
                if (getContent && ReturnContent(sector))
                    yield return (address[index], true);
            if (sector >= returnEnd)
                yield break;

            // 二级索引
            for (; index < 8 && sector < returnEnd; ++index)
            {
                int[] buffer = getSector(address[index]);
                if (getAddress && ReturnAddress(index))
                    yield return (address[index], false);
                for (int j = 0; j < buffer.Length && sector < returnEnd; ++j, ++sector)
                    if (getContent && ReturnContent(sector))
                        yield return (buffer[j], true);
            }
            if (sector >= returnEnd)
                yield break;

            // 三级索引
            for (; index < 10 && sector < returnEnd; ++index)
            {
                int[] buffer = getSector(address[index]);
                if (getAddress && ReturnAddress(index))
                    yield return (address[index], false);
                for (int page = 0; page < buffer.Length && sector < returnEnd; ++page)
                {
                    int[] buffer2 = getSector(buffer[page]);
                    if (getAddress && ReturnAddress(index, page))
                        yield return (buffer[page], false);
                    for (int k = 0; k < buffer2.Length && sector < returnEnd; ++k, ++sector)
                        if (getContent && ReturnContent(sector))
                            yield return (buffer2[k], true);
                }
            }

            // 判断是否需要返回该内容扇区
            bool ReturnContent(int s) => s >= returnStart && s < returnEnd;
            // 判断是否需要返回该索引扇区
            bool ReturnAddress(int i, int p = -1)
            {
                const int PAGE_SIZE = DiskManager.INT_PER_SECTOR;
                // 定义索引记录的扇区范围为[addressStart, addressEnd]
                int addressStart, addressEnd;
                switch (i)
                {
                case 6 or 7:
                    // 二级索引扇区范围
                    i -= 6;
                    addressStart = i * PAGE_SIZE + 6;
                    addressEnd = addressStart + PAGE_SIZE;
                    break;
                case 8 or 9:
                    // 三级索引扇区范围
                    i -= 8;
                    if (p < 0)
                    {
                        addressStart = i * PAGE_SIZE * PAGE_SIZE + 2 * PAGE_SIZE + 6;
                        addressEnd = addressStart + PAGE_SIZE * PAGE_SIZE;
                    }
                    // 三级索引下的二级索引范围
                    else
                    {
                        addressStart = i * PAGE_SIZE * PAGE_SIZE + 2 * PAGE_SIZE + 6 + p * PAGE_SIZE;
                        addressEnd = addressStart + PAGE_SIZE;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(i));
                }
                return addressEnd > returnStart && addressStart < returnEnd;
            }
        }
    }

    /// <summary>
    /// 文件路径搜索相关工具
    /// </summary>
    internal static class PathUtility
    {
        /// <summary>
        /// 判断一个路径字符串是否为目录
        /// </summary>
        public static bool IsDirectory(string path) => path.EndsWith('/');

        public static string GetFileName(string path) => path.Split('/').Last();

        public static string ToFilePath(string path) => path.TrimEnd('/');
        public static string ToDirectoryPath(string path) => path + (IsDirectory(path) ? "" : "/");

        public static string Join(string path1, string path2) => ToDirectoryPath(path1) + path2;
    }
}
