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
        private readonly IInodeManager m_InodeManager;      // Inode管理器
        private readonly IUserManager m_UserManager;        // 用户管理器

        public FileManager(ISectorManager sectorManager, IInodeManager inodeManager, IUserManager userManager)
        {
            m_SectorManager = sectorManager;
            m_UserManager = userManager;
            m_InodeManager = inodeManager;
        }

        public OpenFile Open(string path)
        {
            Inode inode = m_InodeManager.GetInode(GetFileInode(path, m_UserManager.Current));
            // 打开文件即申请Inode使用权
            return new OpenFile(inode);
        }

        public void Close(OpenFile file)
        {
            // 关闭打开文件即归还Inode使用权
            m_InodeManager.PutInode(file.inode.number);
        }

        public void CreateFile(string path)
        {
            if (PathUtility.IsDirectory(path))
                throw new ArgumentException("路径不能是目录：" + path);
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
            // 写入新的文件目录项
            AddEntry(dirInode, new Entry(inode.number, PathUtility.GetFileName(path)));

        }

        public void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(OpenFile file, byte[] buffer, int size)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(OpenFile file, byte[] buffer, int size)
        {
            throw new NotImplementedException();
        }

        public void ReadStruct<T>(OpenFile file, out T value) where T : unmanaged
        {
            // 读取结构体只能在单个扇区中读取
            using Sector sector = m_SectorManager.GetSector(file.inode.GetUsedSector(file.pointer, m_SectorManager));
            m_SectorManager.ReadStruct(sector, out value, file.pointer % DiskManager.SECTOR_SIZE);
            file.pointer += Marshal.SizeOf<T>();
        }

        public void WriteStruct<T>(OpenFile file, ref T value) where T : unmanaged
        {
            // 写入位置超出文件范围且刚好在扇区末尾时 需要一个新扇区
            int sectorNo = file.pointer != file.inode.size && file.pointer % DiskManager.SECTOR_SIZE == 0 ?
                file.inode.GetUsedSector(file.pointer, m_SectorManager) : 
                m_SectorManager.GetEmptySector();
            // 写入结构体只能写入在单个扇区中
            using Sector sector = m_SectorManager.GetSector(sectorNo);
            m_SectorManager.WriteStruct(sector, ref value, file.pointer % DiskManager.SECTOR_SIZE);
            // 更新文件指针和文件大小
            file.pointer += Marshal.SizeOf<T>();
            file.inode.size = Math.Max(file.inode.size, file.pointer);
        }

        public void Seek(OpenFile file, int pos, SeekType type = SeekType.Begin)
        {
            int target = type switch
            {
                SeekType.Begin => pos,
                SeekType.Current => file.pointer + pos,
                SeekType.End => file.inode.size + pos,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            if (target < 0 || target > file.inode.size)
                throw new ArgumentOutOfRangeException(nameof(pos), pos, "文件指针超出文件范围");
            file.pointer = target;
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
            foreach (int sectorNo in dirInode.GetUsedContentSectors(m_SectorManager))
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
        // 添加目录项
        private void AddEntry(Inode dirInode, Entry entry)
        {
            OpenFile file = new(dirInode);
            Seek(file, 0, SeekType.End);
            DirectoryEntry de = entry.ToDirectoryEntry();
            WriteStruct(file, ref de);
            // 更新Inode
            file.inode.uid = (short)m_UserManager.Current.UserId;
            m_InodeManager.UpdateInode(file.inode.number, file.inode);
        }
        #endregion
    }
}
