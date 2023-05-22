using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 打开文件结构
    /// </summary>
    internal class OpenFile
    {
        public readonly Inode inode;    // 文件对应的Inode
        public int pointer;             // 读写指针在文件中的位置

        public OpenFile(Inode inode)
        {
            this.inode = inode;
            pointer = 0;
        }

        /// <summary>
        /// 申请写入文件所需的所有新扇区权限并返回
        /// </summary>
        /// <param name="newSize">新文件大小 单位：字节</param>
        /// <param name="prevSize">原文件大小 单位：字节</param>
        /// <param name="sectorManager">需要使用的扇区管理器</param>
        public static List<Sector> GetWritingSectors(int newSize, int prevSize, ISectorManager sectorManager)
        {
            // 计算需要的所有新扇区数量与内容新扇区数量
            int sectorCount = DiskManager.GetSectorCount(newSize) - DiskManager.GetSectorCount(prevSize);
            // 获取所有新扇区的写入权限 保证可完成写入
            List<Sector> sectors = new(sectorCount);
            try
            {
                for (int i = 0; i < sectorCount; i++)
                    sectors.Add(sectorManager.GetSector(sectorManager.GetEmptySector()));
            }
            catch
            {
                // 未能找到所需的盘块时 释放所有已获得的资源
                sectors.ForEach(sector => sector.Dispose());
                throw;
            }
            // 返回扇区
            return sectors;
        }
    }
}
