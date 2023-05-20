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
        /// <param name="size">获取内容量大小 单位：字节</param>
        /// <param name="getSector">获取盘块的方法 一般需要使用BlockManager</param>
        /// <param name="start">获取内容量起始位置 单位：字节</param>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public static IEnumerable<(int sectorNo, bool content)> GetUsedSectors(IReadOnlyList<int> address, int size, Func<int, int[]> getSector, int start = 0)
        {
            int sector = size / DiskManager.SECTOR_SIZE + (size % DiskManager.SECTOR_SIZE == 0 ? 0 : 1);
            int returnStart = sector - start / DiskManager.SECTOR_SIZE;
            // 一级索引
            int index = 0;
            for (; index < 6 && sector > 0; ++index, --sector)
                if (sector <= returnStart)
                    yield return (address[index], true);
            if (sector <= 0)
                yield break;

            // 二级索引
            for (; index < 8 && sector > 0; ++index)
            {
                int[] buffer = getSector(address[index]);
                yield return (address[index], false);
                for (int j = 0; j < buffer.Length && sector > 0; ++j, --sector)
                    if (sector <= returnStart)
                        yield return (buffer[j], true);
            }
            if (sector <= 0)
                yield break;

            // 三级索引
            for (; index < 10 && sector > 0; ++index)
            {
                int[] buffer = getSector(address[index]);
                yield return (address[index], false);
                for (int j = 0; j < buffer.Length && sector > 0; ++j)
                {
                    int[] buffer2 = getSector(buffer[j]);
                    yield return (buffer[j], false);
                    for (int k = 0; k < buffer2.Length && sector > 0; ++k, --sector)
                        if (sector <= returnStart)
                            yield return (buffer2[k], true);
                }
            }
        }
        /// <summary>
        /// 使用BufferManager根据Inode地址项获取其使用的所有盘块
        /// </summary>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public static IEnumerable<(int sectorNo, bool content)> GetUsedSectors(IReadOnlyList<int> address, int size, ISectorManager sectorManager, int start = 0)
        {
            return GetUsedSectors(address, size, (sectorNo) =>
            {
                int[] ret = new int[DiskManager.SECTOR_SIZE / sizeof(int)];
                using Sector sector = sectorManager.GetSector(sectorNo);
                sectorManager.ReadArray(sector, ret, 0, ret.Length);
                return ret;
            }, start);
        }
        /// <summary>
        /// 使用BufferManager 根据Inode地址项 获取其使用的所有实际内容盘块
        /// </summary>
        public static IEnumerable<int> GetUsedContentSectors(IReadOnlyList<int> address, int size, ISectorManager sectorManager, int start = 0)
        {
            return GetUsedSectors(address, size, sectorManager, start).Where(x => x.content).Select(x => x.sectorNo);
        }
        /// <summary>
        /// 使用BufferManager 根据Inode地址项 获取指定字节数位置的一个盘块
        /// </summary>
        public static int GetUsedSector(IReadOnlyList<int> address, int position, ISectorManager sectorManager)
        {
            try
            {
                return GetUsedContentSectors(address, 1, sectorManager, position).First();
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException($"未找到字节 {position} 处的盘块", e);
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
    }
}
