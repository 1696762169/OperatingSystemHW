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
        private readonly IInodeManager m_InodeManager;    // Inode管理器
        private readonly IUserManager m_UserManager;      // 用户管理器

        public FileManager(ISectorManager sectorManager, IInodeManager inodeManager, IUserManager userManager)
        {
            m_SectorManager = sectorManager;
            m_UserManager = userManager;
            m_InodeManager = inodeManager;
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
            // 检查目标文件夹是否存在

            // 检查是否已经有重名文件/目录
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

        // 根据路径查找Inode序号 不存在返回0
        private int GetDirInode(string path, User user)
        {
            if (string.IsNullOrEmpty(path))
                return 0;
            // 判断查找起点
            int cur = path[0] == '/' ? user.HomeNo : user.CurrentNo;

            // 查找所有路径项
            List<string> pathItems = new(path.Split('/').Where((str) => !string.IsNullOrEmpty(str)));
            if (pathItems.Count == 0)
                return cur;
            pathItems.RemoveAt(pathItems.Count - 1);
            foreach (string item in pathItems)
            {
                // 读取当前目录
                Inode inode = m_InodeManager.GetInode(cur);
                //if (inode == null)
                //    return 0;
                //// 读取目录内容
                //Directory dir = new();
                //m_SectorManager.Read(dir.Data, inode.DataBlockNo, 0, inode.Size);
                //// 查找目录项
                //int i = 0;
                //for (; i < dir.Count; i++)
                //{
                //    if (dir[i].Name == item)
                //        break;
                //}
                //// 未找到
                //if (i == dir.Count)
                //    return 0;
                //// 找到
                //cur = dir[i].InodeNo;
            }
            return 0;
        }
    }
}
