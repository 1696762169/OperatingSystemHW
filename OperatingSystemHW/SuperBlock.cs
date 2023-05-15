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
        //public const int FREE_SECTOR_CAPACITY = 120;    // 直接管理的空闲盘块最大数量
        //public const int INODE_CAPACITY = 120;  // 直接管理的空闲外存Inode最大数量
        //private int m_FreeCount;    // 直接管理的空闲盘块数
        //private unsafe fixed int m_FreeSectors[FREE_SECTOR_CAPACITY];    // 直接管理的空闲盘块索引表
        //private int m_InodeCount;   // 直接管理的空闲外存Inode数
        //private unsafe fixed int m_Inodes[INODE_CAPACITY];   // 直接管理的空闲外存Inode索引表

        public const int SIZE = DiskManager.SUPER_BLOCK_SIZE * DiskManager.SECTOR_SIZE; // 超级块大小
        public const int MAX_USER_COUNT = SIZE / DiskUser.SIZE - 1; // 最大用户数

        public const int SIGNATURE_SIZE = 12;   // 签名区大小
        public const int PADDING_SIZE = SIZE - MAX_USER_COUNT * DiskUser.SIZE - sizeof(int) * 5 - SIGNATURE_SIZE;	// 填充区大小

        public unsafe fixed byte users[MAX_USER_COUNT * DiskUser.SIZE]; // 用户信息表
        private int m_UserCount;    // 用户数
        public int UserCount => m_UserCount;

        private int m_Modified; // 被更改标识
        private int m_ModifyTime;   // 最近一次更新时间
        public int Modified => m_Modified;
        public int ModifyTime => m_ModifyTime;

        public int m_FreeSector;    // 数据区空闲盘块数
        public int m_DataSector;    // 数据区总盘快数
        public int FreeCount
        {
            get => m_FreeSector;
            set
            {
                m_FreeSector = value;
                Modify();
            }
        }
        public int DataSector => m_DataSector;

        public unsafe fixed byte signature[SIGNATURE_SIZE];	// 签名区
        private unsafe fixed byte m_Padding[PADDING_SIZE];	// 填充区

        /// <summary>
        /// 初始化超级块 未签名
        /// </summary>
        public static SuperBlock Init()
        {
            SuperBlock sb = new()
            {
                m_UserCount = 1,
                m_Modified = 0,
                m_ModifyTime = Utility.Time,
                m_FreeSector = DiskManager.DATA_SIZE,
                m_DataSector = DiskManager.DATA_SIZE,
            };

            // 添加超级用户
            DiskUser super = DiskUser.NewSuperUser();
            unsafe
            {
                Marshal.StructureToPtr(super, (IntPtr)sb.users, false);
            }
            return sb;
        }

        /// <summary>
        /// 读取签名
        /// </summary>
        public string GetSignature()
        {
            unsafe
            {
                fixed (byte* p = signature)
                {
                    return Utility.DecodeString(p, SIGNATURE_SIZE);
                }
            }
        }

        /// <summary>
        /// 设置签名
        /// </summary>
        public void SetSignature(string sign)
        {
            unsafe
            {
                fixed (byte* p = signature)
                {
                    Marshal.Copy(Utility.EncodeString(sign, SIGNATURE_SIZE), 0, (IntPtr)p, SIGNATURE_SIZE);
                }
            }
            Modify();
        }

        /// <summary>
        /// 获取一个用户信息
        /// </summary>
        public DiskUser GetUser(int index)
        {
            unsafe
            {
                fixed (byte* up = this.users)
                {
                    return *(DiskUser*)(up + index * DiskUser.SIZE);
                }
            }
        }
        /// <summary>
        /// 设置一个用户信息
        /// </summary>
        public void SetUser(int index, DiskUser user)
        {
            unsafe
            {
                fixed (byte* up = this.users)
                {
                    *(DiskUser*)(up + index * DiskUser.SIZE) = user;
                }
            }
            Modify();
        }

        private void Modify()
        {
            m_Modified = 1;
            m_ModifyTime = Utility.Time;
        }
    }
}
