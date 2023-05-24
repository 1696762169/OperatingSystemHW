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
        public ref SuperBlock Sb => ref m_SuperBlock;

        private SuperBlock m_SuperBlock;
        private readonly byte[] m_UpdateBuffer = new byte[SuperBlock.NO_USER_SIZE];

        private readonly User[] m_Users = new User[SuperBlock.MAX_USER_COUNT]; // 用户信息
        private readonly Dictionary<int, int> m_IdToIndex = new();  // 用户ID到用户序号的映射表

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
                        m_DiskManager.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
                    }
                }
            }

            // 读取用户信息
            for (int i = 0; i < m_SuperBlock.UserCount; i++)
            {
                DiskUser diskUser = m_SuperBlock.GetUser(i);
                if (diskUser.uid == 0)
                    continue;
                m_Users[i] = new User(diskUser);
                m_IdToIndex[diskUser.uid] = i;
            }
        }

        /// <summary>
        /// 将超级块中的非用户内容写入文件
        /// </summary>
        public void UpdateSuperBlock()
        {
            unsafe
            {
                fixed (SuperBlock* sb = &m_SuperBlock)
                {
                    Marshal.Copy((IntPtr)((byte*)sb + SuperBlock.NO_USER_START), m_UpdateBuffer, 0, SuperBlock.NO_USER_SIZE);
                    m_DiskManager.WriteBytes(m_UpdateBuffer, DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE + SuperBlock.NO_USER_START);
                }
            }
        }

        public User GetUser(int uid)
        {
            if (!CheckUserIndex(m_IdToIndex[uid]))
                throw new ArgumentOutOfRangeException(nameof(uid));
            return m_Users[m_IdToIndex[uid]];
        }

        public void UpdateUser(int uid)
        {
            if (!CheckUserIndex(m_IdToIndex[uid]))
                throw new ArgumentOutOfRangeException(nameof(uid));
            m_SuperBlock.SetUser(m_IdToIndex[uid], m_Users[m_IdToIndex[uid]].ToDiskUser());
            m_DiskManager.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
        }

        public void SetUser(User user, int uid)
        {
            if (!CheckUserIndex(m_IdToIndex[uid]))
                throw new ArgumentOutOfRangeException(nameof(uid));
            m_Users[m_IdToIndex[uid]] = user;
            m_SuperBlock.SetUser(m_IdToIndex[uid], m_Users[m_IdToIndex[uid]].ToDiskUser());
            m_DiskManager.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
        }

        private static bool CheckUserIndex(int index) => index is >= 0 and < SuperBlock.MAX_USER_COUNT;
    }
}
