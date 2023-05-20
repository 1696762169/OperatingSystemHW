using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
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

        public void CreateFile(string path)
        {
            // 获取目标文件夹
            using Inode dirInode = m_InodeManager.GetInode(GetDirInode(path, m_UserManager.Current));

            // 检查是否已经有重名文件
            try
            {
                GetFileInode(dirInode, PathUtility.GetFileName(path));
                throw new ArgumentException("已经有重名文件：" + path);
            }
            catch (FileNotFoundException)
            {
                // 没有重名文件，继续
            }
            
            // 获取一个空闲Inode
            using Inode inode = m_InodeManager.GetEmptyInode();
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

        public IEnumerable<Entry> GetEntries()
        {
            using Inode inode = m_InodeManager.GetInode(m_UserManager.Current.CurrentNo);
            return GetEntries(inode);
        }

        #region 辅助函数
        // 根据路径查找文件Inode序号
        private int GetFileInode(string path, User user)
        {
            if (string.IsNullOrEmpty(path) || PathUtility.IsDirectory(path))
                throw new ArgumentException("路径不能为空或者是目录");

            using Inode dirInode = m_InodeManager.GetInode(GetDirInode(path, user));
            return GetFileInode(dirInode, PathUtility.GetFileName(path));
        }
        // 根据目录Inode和文件名查找文件Inode序号
        private int GetFileInode(Inode dirInode, string fileName)
        {
            if (dirInode == null)
                throw new ArgumentNullException(nameof(dirInode));
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("文件名不能为空");
            foreach (Entry entry in GetEntries(dirInode))
            {
                if (entry.name != fileName)
                    continue;
                return entry.inodeNo;
            }
            throw new FileNotFoundException($"未找到文件：{fileName}");
        }
        // 根据路径查找目录Inode序号
        private int GetDirInode(string path, User user)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("路径不能为空");
            // 判断查找起点
            int cur = path[0] == '/' ? user.HomeNo : user.CurrentNo;

            // 查找所有路径项
            List<string> pathItems = new(path.Split('/').Where(str => !string.IsNullOrEmpty(str)));
            if (!PathUtility.IsDirectory(path) && pathItems.Count >= 1) // 移除文件名
                pathItems.RemoveAt(pathItems.Count - 1);
            if (pathItems.Count == 0)   // 没有下级目录则返回查找起点
                return cur;
            foreach (string item in pathItems)
            {
                // 读取当前目录
                using Inode inode = m_InodeManager.GetInode(cur);

                // 查找目录内容
                bool find = false;
                foreach (Entry entry in GetEntries(inode))
                {
                    if (entry.name != item)
                        continue;
                    cur = entry.inodeNo;
                    find = true;
                    break;
                }

                if (!find)
                    throw new DirectoryNotFoundException($"未找到目录：{string.Join('/', pathItems) + '/'}");
            }
            return cur;
        }

        // 查找目录中的所有目录项
        private IEnumerable<Entry> GetEntries(Inode dirInode)
        {
            int size = dirInode.size;
            DirectoryEntry[] buffer = new DirectoryEntry[DiskManager.SECTOR_SIZE / Marshal.SizeOf<DirectoryEntry>()];
            foreach (int sectorNo in Utility.GetUsedContentSectors(dirInode.address, dirInode.size, m_SectorManager))
            {
                // 获取扇区权限
                using Sector sector = m_SectorManager.GetSector(sectorNo);

                // 读取剩余数量的目录项
                int readNumber = Math.Min(size, DiskManager.SECTOR_SIZE / Marshal.SizeOf<DirectoryEntry>());
                m_SectorManager.ReadArray(sector, buffer, 0, readNumber);
                for (int i = 0; i < readNumber; ++i)
                    yield return new Entry(buffer[i]);
                size -= readNumber;
            }
        }
        #endregion
    }
}
