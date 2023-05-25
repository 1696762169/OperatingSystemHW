using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件块结构
    /// </summary>
    internal class Sector : IDisposable
    {
        public readonly int number;         // 文件块逻辑序号

        private readonly ISectorManager m_SectorManager;    // 用于资源释放的SectorManager
        public Sector(int number, ISectorManager sectorManager)
        {
            this.number = number;
            m_SectorManager = sectorManager;
        }

        public void Dispose()
        {
            m_SectorManager.PutSector(this);
        }
    }
}
