using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 缓存块状态标志位
    /// </summary>
    internal enum BufferFlag
    {
        Write = 0x1,        // 写操作。将缓存中的信息写到硬盘上去
        Read = 0x2,         // 读操作。从盘读取信息到缓存中
        Done = 0x4,         // I/O操作结束
        Error = 0x8,        // I/O因出错而终止
        Busy = 0x10,        // 相应缓存正在使用中
        Wanted = 0x20,      // 有进程正在等待使用该buf管理的资源，清B_BUSY标志时，要唤醒这种进程
        Async = 0x40,       // 异步I/O，不需要等待其结束
        DelayWrite = 0x80   // 延迟写，在相应缓存要移做他用时，再将其内容写到相应块设备上
    }

    /// <summary>
    /// 文件块结构
    /// </summary>
    internal class Sector : IDisposable
    {
        public int Number { get; private set; }         // 文件块逻辑序号

        private readonly ISectorManager m_SectorManager;    // 用于资源释放的SectorManager
        public Sector(int number, ISectorManager sectorManager)
        {
            Number = number;
            m_SectorManager = sectorManager;
        }

        public void Dispose()
        {
            m_SectorManager.PutSector(this);
        }
    }
}
