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
            List<Sector> sectors = WritePrepare(file, Marshal.SizeOf<T>());
            try
            {
                // 写入结构体只能写入在单个扇区中
                m_SectorManager.WriteStruct(sectors[0], ref value, file.pointer % DiskManager.SECTOR_SIZE);
                file.pointer += Marshal.SizeOf<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"写入结构体失败：\n{e.Message}");
                throw;
            }
            finally
            {
                sectors.ForEach(sector => sector.Dispose());
            }
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

        #region 公共实现
        /// <summary>
        /// 申请需要写入内容得扇区权限 并更新文件索引与大小
        /// </summary>
        /// <param name="file">打开文件结构</param>
        /// <param name="writeSize">写入内容大小</param>
        private List<Sector> WritePrepare(OpenFile file, int writeSize)
        {
            // 申请新扇区权限
            int newSize = Math.Max(file.pointer + writeSize, file.inode.size);
            List<Sector> newSectors;
            try
            {
                newSectors = OpenFile.GetWritingSectors(newSize, file.inode.size, m_SectorManager);
            }
            catch (Exception e)
            {
                Console.WriteLine($"申请新扇区失败：{e.Message}");
                throw;
            }
            // 申请需要写入的原内容权限
            List<Sector> contentSectors = new();
            try
            {
                contentSectors.AddRange(file.inode.GetUsedContentSectors(m_SectorManager, file.pointer, writeSize)
                    .Select(m_SectorManager.GetSector));
            }
            catch (Exception e)
            {
                Console.WriteLine($"申请待写入原扇区失败：{e.Message}");
                newSectors.ForEach(sector => sector.Dispose());
                contentSectors.ForEach(sector => sector.Dispose());
                throw;
            }
            // 申请索引扇区权限
            List<Sector> addressSectors = new();
            try
            {
                // 此处为后续简化起见 申请了所有索引扇区权限 即使是不会更改的索引扇区
                addressSectors.AddRange(file.inode.GetUsedAddressSectors(m_SectorManager)
                    .Select(m_SectorManager.GetSector));
            }
            catch (Exception e)
            {
                Console.WriteLine($"申请索引扇区失败：{e.Message}");
                newSectors.ForEach(sector => sector.Dispose());
                contentSectors.ForEach(sector => sector.Dispose());
                addressSectors.ForEach(sector => sector.Dispose());
                throw;
            }

            // 更新索引
            try
            {
                contentSectors.AddRange(file.inode.UpdateAddress(newSize, file.inode.size, m_SectorManager, newSectors, addressSectors));
            }
            catch (Exception e)
            {
                Console.WriteLine($"更新Inode索引失败：{e.Message}");
                newSectors.ForEach(sector => sector.Dispose());
                contentSectors.ForEach(sector => sector.Dispose());
                addressSectors.ForEach(sector => sector.Dispose());
                throw;
            }
            // 更新文件大小
            file.inode.size = newSize;

            return contentSectors;
        }
        #endregion

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
            int readCount = dirInode.size / Marshal.SizeOf<DirectoryEntry>();
            DirectoryEntry[] buffer = new DirectoryEntry[DiskManager.SECTOR_SIZE / Marshal.SizeOf<DirectoryEntry>()];
            foreach (int sectorNo in dirInode.GetUsedContentSectors(m_SectorManager))
            {
                // 获取扇区权限
                using Sector sector = m_SectorManager.GetSector(sectorNo);

                // 读取剩余数量的目录项
                int readNumber = Math.Min(readCount, DiskManager.SECTOR_SIZE / Marshal.SizeOf<DirectoryEntry>());
                m_SectorManager.ReadArray(sector, buffer, 0, readNumber);
                for (int i = 0; i < readNumber; ++i)
                    yield return new Entry(buffer[i]);
                readCount -= readNumber;
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
