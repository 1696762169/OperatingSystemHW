using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件块管理器接口
    /// </summary>
    internal interface IBlockManager
    {
        /// <summary>
        /// 获取一个空闲盘块的控制权
        /// </summary>
        public Block GetFreeBlock();

        /// <summary>
        /// 获取一个指定盘块的控制权
        /// </summary>
        public Block GetBlock(int blockNo);
        /// <summary>
        /// 归还一个指定盘块的控制权
        /// </summary>
        public void PutBlock(int blockNo);

        /// <summary>
        /// 读取一个盘块中的内容
        /// </summary>
        public void ReadBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0);
        /// <summary>
        /// 向一个盘块中写入内容
        /// </summary>
        public void WriteBlock(Block block, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int offset = 0);
    }
}
