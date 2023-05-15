using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 全局数据管理类 某种意义上来说应该叫做SuperBlockManager
    /// </summary>
    internal class FileSystem : ISuperBlockManager, IUserManager
    {
        public SuperBlock Sb => m_SuperBlock;   // 超级块
        private SuperBlock m_SuperBlock;

        private readonly User[] m_Users = new User[SuperBlock.MAX_USER_COUNT]; // 用户信息

        private readonly IDiskManager m_DiskManager;  // 磁盘管理器

        public FileSystem(IDiskManager diskManager)
        {
            m_DiskManager = diskManager;
            // 读取超级块
            m_DiskManager.Read(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, out m_SuperBlock);
            // 若签名不正确 则重新初始化超级块
            unsafe
            {
                fixed (SuperBlock* sb = &m_SuperBlock)
                {
                    if (Utility.DecodeString(sb->signature, SuperBlock.SIGNATURE_SIZE) != "Made by JYX")
                    {
                        m_SuperBlock = SuperBlock.Init();
                        UpdateSuperBlock();
                    }
                }
            }

            // 读取用户信息
            for (int i = 0; i < m_SuperBlock.UserCount; i++)
            {
                DiskUser diskUser = m_SuperBlock.GetUser(i);
                if (diskUser.uid != 0)
                    m_Users[i] = new User(diskUser);
            }
        }

        /// <summary>
        /// 将超级块内容写入文件
        /// </summary>
        public void UpdateSuperBlock()
        {
            m_DiskManager.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
        }

        public User GetUser(int index)
        {
            if (!CheckUserIndex(index))
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Users[index];
        }

        public void UpdateUser(int index)
        {
            if (!CheckUserIndex(index))
                throw new ArgumentOutOfRangeException(nameof(index));
            m_SuperBlock.SetUser(index, m_Users[index].ToDiskUser());
        }

        public void SetUser(User user, int index)
        {
            if (!CheckUserIndex(index))
                throw new ArgumentOutOfRangeException(nameof(index));
            m_Users[index] = user;
            m_SuperBlock.SetUser(index, m_Users[index].ToDiskUser());
        }

        private static bool CheckUserIndex(int index) => index is >= 0 and < SuperBlock.MAX_USER_COUNT;
    }
}
