using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件操作接口类
    /// </summary>
    internal class FileManager : IFileManager
    {
        private readonly ISectorManager m_SectorManager;    // 文件块管理器
        private readonly IUserManager m_UserManager;      // 用户管理器

        public FileManager(ISectorManager sectorManager, IUserManager userManager)
        {
            m_SectorManager = sectorManager;
            m_UserManager = userManager;
        }

        public OpenFile Open(string path)
        {
            throw new NotImplementedException();
        }

        public void Close(OpenFile file)
        {
            throw new NotImplementedException();
        }

        public void Create(string path)
        {
            throw new NotImplementedException();
        }

        public void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public void Read(OpenFile file, byte[] buffer, int size)
        {
            throw new NotImplementedException();
        }

        public void Write(OpenFile file, byte[] buffer, int size)
        {
            throw new NotImplementedException();
        }

        public void Seek(OpenFile file, int pos, SeekType type = SeekType.Begin)
        {
            throw new NotImplementedException();
        }
    }
}
