using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 超级块 定义文件系统的全局变量
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct SuperBlock
    {
        public const int FREE_SECTOR_CAPACITY = 120;    // 直接管理的空闲盘块最大数量
        public const int INODE_CAPACITY = 120;  // 直接管理的空闲外存Inode最大数量
        public const int PADDING_SIZE = 2 * DiskManager.SECTOR_SIZE / 4 - FREE_SECTOR_CAPACITY - INODE_CAPACITY - 2 - 3;	// 填充区大小

        private int m_FreeCount;    // 直接管理的空闲盘块数
        private unsafe fixed int m_FreeSectors[FREE_SECTOR_CAPACITY];    // 直接管理的空闲盘块索引表

        private int m_InodeCount;   // 直接管理的空闲外存Inode数
        private unsafe fixed int m_Inodes[INODE_CAPACITY];   // 直接管理的空闲外存Inode索引表

        private int m_Modified; // 被更改标识
        private long m_ModifyTime;   // 最近一次更新时间

        private unsafe fixed int m_Padding[PADDING_SIZE];	// 填充区

        /// <summary>
        /// 初始化超级块
        /// </summary>
        public static SuperBlock Init()
        {
            SuperBlock sb = new()
            {
                m_FreeCount = FREE_SECTOR_CAPACITY,
                m_InodeCount = INODE_CAPACITY,
                m_Modified = 0,
                m_ModifyTime = DateTime.Now.ToBinary(),
            };

            unsafe
            {
                for (int i = 0; i < INODE_CAPACITY; i++)
                {
                    sb.m_Inodes[i] = i + 1;
                }
                for (int i = 0; i < FREE_SECTOR_CAPACITY; i++)
                {
                    sb.m_FreeSectors[i] = i + 1;
                }
            }

            return sb;
        }
    }
}
