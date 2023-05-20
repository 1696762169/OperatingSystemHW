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
    internal interface ISectorManager
    {
        /// <summary>
        /// 获取空闲盘块的控制权
        /// </summary>
        /// <param name="count">需要获取的盘块数</param>
        public IEnumerable<Sector> GetEmptySector(int count);
        /// <summary>
        /// 获取一个空闲盘块的控制权
        /// </summary>
        public Sector GetEmptySector();

        /// <summary>
        /// 获取一个指定盘块的控制权
        /// </summary>
        public Sector GetSector(int sectorNo);
        /// <summary>
        /// 归还一个指定盘块的控制权
        /// </summary>
        public void PutSector(Sector sector);

        /// <summary>
        /// 读取一个盘块中的内容
        /// </summary>
        public void ReadBytes(Sector sector, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int position = 0);
        /// <summary>
        /// 向一个盘块中写入内容
        /// </summary>
        public void WriteBytes(Sector sector, byte[] buffer, int size = DiskManager.SECTOR_SIZE, int position = 0);

        /// <summary>
        /// 在指定位置读取结构体 其大小不得超过盘块大小
        /// </summary>
        public void ReadStruct<T>(Sector sector, out T value, int position = 0) where T : unmanaged;
        /// <summary>
        /// 在指定位置写入结构体 其大小不得超过盘块大小
        /// </summary>
        public void WriteStruct<T>(Sector sector, ref T value, int position = 0) where T : unmanaged;

        /// <summary>
        /// 读取数组 总大小不得超过盘块大小
        /// </summary>
        /// <param name="sector">读取起始位置</param>
        /// <param name="array">数据存储数组</param>
        /// <param name="offset">数组序号偏移量</param>
        /// <param name="count">读取元素数量</param>
        /// <param name="position"></param>
        public void ReadArray<T>(Sector sector, T[] array, int offset, int count, int position = 0) where T : unmanaged;

        /// <summary>
        /// 写入数组 总大小不得超过盘块大小
        /// </summary>
        /// <param name="sector">读取起始位置</param>
        /// <param name="array">数据存储数组</param>
        /// <param name="offset">数组序号偏移量</param>
        /// <param name="count">写入元素数量</param>
        /// <param name="position"></param>
        public void WriteArray<T>(Sector sector, T[] array, int offset, int count, int position = 0) where T : unmanaged;

        /// <summary>
        /// 清除扇区中的数据
        /// </summary>
        /// <param name="sectors"></param>
        public void ClearSector(params Sector[] sectors);
    }
}
