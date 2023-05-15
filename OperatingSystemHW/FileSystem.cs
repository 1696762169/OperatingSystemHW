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
        }

        /// <summary>
        /// 将超级块内容写入文件
        /// </summary>
        public void UpdateSuperBlock()
        {
            m_DiskManager.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
        }

        public string GetSignature()
        {
            unsafe
            {
                fixed (byte* p = m_SuperBlock.signature)
                {
                    return Utility.DecodeString(p, SuperBlock.SIGNATURE_SIZE);
                }
            }
        }

        public void SetSignature(string signature)
        {
            unsafe
            {
                fixed (byte* p = m_SuperBlock.signature)
                {
                    Marshal.Copy(Utility.EncodeString(signature, SuperBlock.SIGNATURE_SIZE), 0, (IntPtr)p, SuperBlock.SIGNATURE_SIZE);
                }
            }
        }
    }
}
