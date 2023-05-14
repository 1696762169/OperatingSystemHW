using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 全局数据管理类 某种意义上来说应该叫做SuperBlockManager
    /// </summary>
    internal class FileSystem
    {
        public SuperBlock Sb => m_SuperBlock;   // 超级块
        private SuperBlock m_SuperBlock;

        private IDiskManager m_DiskManager;  // 磁盘管理器

        public FileSystem(IDiskManager diskManager)
        {
            m_DiskManager = diskManager;
            // 读取超级块
            //m_DiskManager.accessor.Read(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, out m_SuperBlock);
        }

        /// <summary>
        /// 将超级块内容写入文件
        /// </summary>
        public void UpdateSuperBlock()
        {
            //m_DiskManager.accessor.Write(DiskManager.SUPER_BLOCK_SECTOR * DiskManager.SECTOR_SIZE, ref m_SuperBlock);
        }
    }
}
