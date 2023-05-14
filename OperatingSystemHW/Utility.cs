﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    internal static class Utility
    {
        public static int Time => (int)DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        public static byte[] EncodeString(string str) => Encoding.UTF8.GetBytes(str);
        public static string DecodeString(byte[] data) => Encoding.UTF8.GetString(data);

        /// <summary>
        /// 根据Inode地址项获取其使用的所有盘块
        /// </summary>
        /// <param name="address">地址数组 必须为10位</param>
        /// <param name="size">文件大小</param>
        /// <param name="getBlock">获取盘块的方法 一般需要使用BlockManager</param>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public static IEnumerable<(int blockNo, bool content)> GetUsedBlocks(IReadOnlyList<int> address, int size, Func<int, int[]> getBlock)
        {
            int sector = size / DiskManager.SECTOR_SIZE + (size % DiskManager.SECTOR_SIZE == 0 ? 0 : 1);
            // 一级索引
            int index;
            for (index = 0; index < 6 && sector > 0; ++index, --sector)
                yield return (address[index], true);
            if (sector <= 0)
                yield break;

            // 二级索引
            for (; index < 8 && sector > 0; ++index)
            {
                int[] buffer = getBlock(address[index]);
                yield return (address[index], false);
                for (int j = 0; j < buffer.Length && sector > 0; ++j, --sector)
                    yield return (buffer[j], true);
            }
            if (sector <= 0)
                yield break;

            // 三级索引
            for (; index < 10 && sector > 0; ++index)
            {
                int[] buffer = getBlock(address[index]);
                yield return (address[index], false);
                for (int j = 0; j < buffer.Length && sector > 0; ++j)
                {
                    int[] buffer2 = getBlock(buffer[j]);
                    yield return (buffer[j], false);
                    for (int k = 0; k < buffer2.Length && sector > 0; ++k, --sector)
                        yield return (buffer2[k], true);
                }
            }
        }

        /// <summary>
        /// 使用BufferManager根据Inode地址项获取其使用的所有盘块
        /// </summary>
        /// <returns>blockNo表示盘块序号 content表示该盘块是否包含实际内容</returns>
        public static IEnumerable<(int blockNo, bool content)> GetUsedBlocks(IReadOnlyList<int> address, int size, IBlockManager blockManager)
        {
            return GetUsedBlocks(address, size, (blockNo) =>
            {
                int[] ret = new int[DiskManager.SECTOR_SIZE / sizeof(int)];
                Block block = blockManager.GetBlock(blockNo);
                blockManager.ReadArray(block, ret, 0, ret.Length);
                blockManager.PutBlock(block);
                return ret;
            });
        }
    }
}
