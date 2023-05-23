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

        public IUserManager UserManager => m_UserManager;
        private readonly IUserManager m_UserManager;        // 用户管理器

        public FileManager(ISectorManager sectorManager, IInodeManager inodeManager, IUserManager userManager)
        {
            m_SectorManager = sectorManager;
            m_UserManager = userManager;
            m_InodeManager = inodeManager;
        }

        public OpenFile Open(string path)
        {
            Inode inode = m_InodeManager.GetInode(GetFileInode(path));
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
            string fileName = PathUtility.GetFileName(path);
            if (fileName.Length > DirectoryEntry.NAME_MAX_COUNT)
                throw new ArgumentException("文件名过长");
            // 获取目标文件夹
            using OpenFile dir = Open(m_InodeManager.GetInode(GetDirInode(path)));

            // 检查是否已经有重名文件
            try
            {
                GetFileInode(dir.inode, fileName);
                throw new ArgumentException("已经有重名文件：" + path);
            }
            catch (FileNotFoundException)
            {
                // 没有重名文件，继续
            }
            
            // 获取一个空闲Inode
            using Inode inode = m_InodeManager.GetEmptyInode();

            // 写入新的文件目录项
            AddEntry(dir, new Entry(inode.number, PathUtility.GetFileName(path)));

            // 更新Inode信息
            inode.uid = (short)m_UserManager.Current.UserId;
            m_InodeManager.UpdateInode(inode.number, inode);
        }

        public void DeleteFile(string path)
        {
            if (PathUtility.IsDirectory(path))
                throw new ArgumentException("路径不能是目录：" + path);
            // 获取目标文件夹
            using OpenFile dir = Open(m_InodeManager.GetInode(GetDirInode(path)));

            // 检查是否存在文件
            int fileInodeNo;
            try
            {
                fileInodeNo = GetFileInode(dir.inode, PathUtility.GetFileName(path));
            }
            catch (FileNotFoundException)
            {
                throw new FileNotFoundException("文件不存在：" + path);
            }

            List<Sector> fileSectors = new();
            try
            {
                // 获取文件Inode权限
                using Inode fileInode = m_InodeManager.GetInode(fileInodeNo);
                // 获取文件扇区访问权限
                fileSectors.AddRange(fileInode.GetUsedSectors(m_SectorManager)
                    .Select(pair => m_SectorManager.GetSector(pair.sectorNo)));

                // 删除文件目录项（可能失败）
                RemoveEntry(dir, fileInodeNo);

                // 释放文件占用磁盘空间
                m_SectorManager.ClearSector(fileSectors.ToArray());
                // 释放文件Inode（将文件所有者修改为无效值0）
                fileInode.uid = 0;
                m_InodeManager.UpdateInode(fileInodeNo, fileInode);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new UnauthorizedAccessException($"获取待删除文件扇区失败：{e.Message}");
            }
            finally
            {
                fileSectors.ForEach(sector => sector.Dispose());
            }
        }

        public void CreateDirectory(string path)
        {
            // 检查是否已经有重名目录
            if (DirectoryExists(path))
                throw new ArgumentException("已经有重名目录：" + path);
            string dirName = PathUtility.ToDirectoryPath(PathUtility.GetFileName(PathUtility.ToFilePath(path)));
            if (dirName.Length > DirectoryEntry.NAME_MAX_COUNT)
                throw new ArgumentException("目录名过长");

            // 查找父目录
            path = PathUtility.ToFilePath(path);
            using OpenFile parent = Open(m_InodeManager.GetInode(GetDirInode(path)));

            // 获取一个空闲Inode
            using Inode inode = m_InodeManager.GetEmptyInode();

            // 写入新的文件目录项
            AddEntry(parent, new Entry(inode.number, dirName));

            // 更新Inode信息
            inode.uid = (short)m_UserManager.Current.UserId;
            m_InodeManager.UpdateInode(inode.number, inode);
        }

        public void DeleteDirectory(string path, bool deleteSub)
        {
            // 获取待删除目录的父目录与自身
            if (path.Trim() == "/")
                throw new ArgumentException("无法删除根目录");
            if (path.Trim() == m_UserManager.Current.Home)
                throw new ArgumentException("无法删除用户主目录");

            // 检查是否存在目录
            if (!DirectoryExists(path))
                throw new DirectoryNotFoundException("目录不存在：" + path);

            using Inode dirInode = m_InodeManager.GetInode(GetDirInode(PathUtility.ToDirectoryPath(path)));
            using OpenFile parent = Open(m_InodeManager.GetInode(GetDirInode(PathUtility.ToFilePath(path))));

            // 检查目录是否为空目录
            if (!deleteSub)
            {
                if (GetEntries(dirInode).Any())
                {
                    throw new ArgumentException($"目录不是空目录，不可删除：{path}");
                }
            }

            // 递归删除其子目录与文件
            foreach (Entry entry in GetEntries(dirInode))
            {
                string sub = PathUtility.Join(path, entry.name);
                try
                {
                    if (PathUtility.IsDirectory(entry.name))
                        DeleteDirectory(sub, deleteSub);
                    else
                        DeleteFile(sub);
                }
                catch (UnauthorizedAccessException e)
                {
                    throw new UnauthorizedAccessException($"无法删除子目录/文件 {sub} ：{e.Message}");
                }
            }

            // 删除此目录
            List<Sector> dirSectors = new();
            try
            {
                // 获取文件扇区访问权限
                dirSectors.AddRange(dirInode.GetUsedSectors(m_SectorManager)
                    .Select(pair => m_SectorManager.GetSector(pair.sectorNo)));

                // 删除文件目录项（可能失败）
                RemoveEntry(parent, dirInode.number);

                // 释放文件占用磁盘空间
                m_SectorManager.ClearSector(dirSectors.ToArray());
                // 释放文件Inode（将文件所有者修改为无效值0）
                dirInode.uid = 0;
                m_InodeManager.UpdateInode(dirInode.number, dirInode);
            }
            catch (UnauthorizedAccessException e)
            {
                throw new UnauthorizedAccessException($"获取待删除目录扇区失败：{e.Message}");
            }
            finally
            {
                dirSectors.ForEach(sector => sector.Dispose());
            }
        }

        public void ReadBytes(OpenFile file, byte[] data)
        {
            if (data.Length == 0)
                return;
            List<Sector> sectors = ReadPrepare(file, data.Length);
            const int SECTOR_SIZE = DiskManager.SECTOR_SIZE;
            byte[] buffer = new byte[SECTOR_SIZE];
            try
            {
                // 读入第一块
                int index = 0;
                if (sectors.Count > 0)
                {
                    int readCount = Math.Min(SECTOR_SIZE - file.pointer % SECTOR_SIZE, data.Length);
                    m_SectorManager.ReadBytes(sectors[0], data, readCount, file.pointer % SECTOR_SIZE);
                    index += readCount;
                    file.pointer += readCount;
                }
                // 读入中间块
                for (int i = 1; i < sectors.Count - 1; ++i)
                {
                    m_SectorManager.ReadBytes(sectors[i], buffer);
                    Array.Copy(buffer, 0, data, index, SECTOR_SIZE);
                    index += SECTOR_SIZE;
                    file.pointer += SECTOR_SIZE;
                }
                // 读入最后一块
                if (sectors.Count > 1)
                {
                    int readCount = data.Length - index;
                    m_SectorManager.ReadBytes(sectors[^1], buffer, readCount);
                    Array.Copy(buffer, 0, data, index, readCount);
                    file.pointer += readCount;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"读取二进制数据失败：\n{e.Message}");
                throw;
            }
            finally
            {
                sectors.ForEach(sector => sector.Dispose());
            }
        }

        public void WriteBytes(OpenFile file, byte[] data)
        {
            if (data.Length == 0)
                return;
            List<Sector> sectors = WritePrepare(file, data.Length);
            const int SECTOR_SIZE = DiskManager.SECTOR_SIZE;
            byte[] buffer = new byte[SECTOR_SIZE];
            try
            {
                // 写入第一块
                int index = 0;
                if (sectors.Count > 0)
                {
                    // 此处考虑了仅写入一块且两头都写不满的情况
                    int writeCount = Math.Min(SECTOR_SIZE - file.pointer % SECTOR_SIZE, data.Length);
                    m_SectorManager.ReadBytes(sectors[0], buffer);
                    Array.Copy(data, index, buffer, file.pointer % SECTOR_SIZE, writeCount);
                    m_SectorManager.WriteBytes(sectors[0], buffer);
                    index += writeCount;
                    file.pointer += writeCount;
                }
                // 写入中间块
                for (int i = 1; i < sectors.Count - 1; ++i)
                {
                    Array.Copy(data, index, buffer, 0, SECTOR_SIZE);
                    m_SectorManager.WriteBytes(sectors[i], buffer);
                    index += SECTOR_SIZE;
                    file.pointer += SECTOR_SIZE;
                }
                // 写入最后一块
                if (sectors.Count > 1)
                {
                    int writeCount = data.Length - index;
                    m_SectorManager.ReadBytes(sectors[^1], buffer);
                    Array.Copy(data, index, buffer, 0, writeCount);
                    m_SectorManager.WriteBytes(sectors[^1], buffer);
                    file.pointer += writeCount;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"写入二进制数据失败：\n{e.Message}");
                throw;
            }
            finally
            {
                sectors.ForEach(sector => sector.Dispose());
            }
        }

        public void ReadStruct<T>(OpenFile file, out T value) where T : unmanaged
        {
            List<Sector> sectors = ReadPrepare(file, Marshal.SizeOf<T>());
            try
            {
                // 读取结构体只能在单个扇区中读取
                m_SectorManager.ReadStruct(sectors[0], out value, file.pointer % DiskManager.SECTOR_SIZE);
                file.pointer += Marshal.SizeOf<T>();
            }
            catch (Exception e)
            {
                Console.WriteLine($"读取结构体失败：\n{e.Message}");
                throw;
            }
            finally
            {
                sectors.ForEach(sector => sector.Dispose());
            }
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

        public bool FileExists(string path)
        {
            try
            {
                using Inode dir = m_InodeManager.GetInode(GetDirInode(path));
                GetFileInode(dir, PathUtility.GetFileName(path));
                return true;
            }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }

        public bool DirectoryExists(string path)
        {
            try
            {
                GetDirInode(PathUtility.ToDirectoryPath(path));
                return true;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        public void ChangeDirectory(string path)
        {
            try
            {
                int inodeNo = GetDirInode(PathUtility.ToDirectoryPath(path));
                string temp = PathUtility.ToFilePath(path);
                m_UserManager.Current.ChangeDirectory(inodeNo, PathUtility.ToDirectoryPath(PathUtility.GetFileName(temp)));
                m_UserManager.UpdateUser(m_UserManager.CurrentIndex);
            }
            catch (DirectoryNotFoundException)
            {
                throw new ArgumentException($"未找到路径：{path}");
            }
        }

        #region 公共实现
        /// <summary>
        /// 申请需要读取内容得的扇区权限
        /// </summary>
        /// <param name="file">打开文件结构</param>
        /// <param name="readSize">读取内容大小</param>
        private List<Sector> ReadPrepare(OpenFile file, int readSize)
        {
            if (file.pointer + readSize > file.inode.size)
                throw new ArgumentOutOfRangeException(nameof(readSize), readSize, "读取大小超出文件范围");
            // 申请需要读取的内容权限
            List<Sector> contentSectors = new();
            try
            {
                contentSectors.AddRange(file.inode.GetUsedContentSectors(m_SectorManager, file.pointer, readSize)
                    .Select(m_SectorManager.GetSector));
            }
            catch (Exception e)
            {
                Console.WriteLine($"申请读取内容扇区失败：{e.Message}");
                contentSectors.ForEach(sector => sector.Dispose());
            }
            return contentSectors;
        }

        /// <summary>
        /// 申请需要写入内容的扇区权限 并更新文件索引与大小
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
            m_InodeManager.UpdateInode(file.inode.number, file.inode);
            return contentSectors;
        }
        #endregion

        #region 辅助函数
        // 根据路径查找文件Inode序号
        private int GetFileInode(string path)
        {
            if (string.IsNullOrEmpty(path) || PathUtility.IsDirectory(path))
                throw new ArgumentException("路径不能为空或者是目录");

            using Inode dirInode = m_InodeManager.GetInode(GetDirInode(path));
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
        private int GetDirInode(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("路径不能为空");
            // 判断查找起点
            User user = m_UserManager.Current;
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
                    if (entry.name != PathUtility.ToDirectoryPath(item))
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

        // 缩小文件大小 释放多余扇区
        private List<Sector> CutFile(OpenFile file, int targetSize)
        {
            // 计算扇区删除截止位置
            int cutSize = (targetSize + DiskManager.SECTOR_SIZE - 1) / DiskManager.SECTOR_SIZE * DiskManager.SECTOR_SIZE;
            List<Sector> sectors = new();
            if (cutSize < file.inode.size)
                return sectors;
            try
            {
                foreach ((int sectorNo, bool _) in file.inode.GetUsedSectors(m_SectorManager, cutSize))
                    sectors.Add(m_SectorManager.GetSector(sectorNo));
                return sectors;
            }
            catch (Exception e)
            {
                sectors.ForEach(sector => sector.Dispose());
                throw new Exception($"申请待裁剪扇区失败：\n{e.Message}");
            }
        }

        private static OpenFile Open(Inode inode) => new(inode);
        #endregion

        #region 目录项相关函数
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
        private void AddEntry(OpenFile dir, Entry entry)
        {
            Seek(dir, 0, SeekType.End);
            DirectoryEntry de = entry.ToDirectoryEntry();
            WriteStruct(dir, ref de);
            // 更新Inode
            m_InodeManager.UpdateInode(dir.inode.number, dir.inode);
        }
        // 移除目录项
        private void RemoveEntry(OpenFile dir, int inodeNo)
        {
            // 获取移除位置
            int dirSize = Marshal.SizeOf<DirectoryEntry>();
            int writePosition = GetEntries(dir.inode)
                .TakeWhile(entry => entry.inodeNo != inodeNo)
                .Sum(_ => dirSize);

            // 获取目录文件待裁剪扇区
            List<Sector> cutSectors = new();
            try
            {
                cutSectors.AddRange(CutFile(dir, dir.inode.size - dirSize));

                // 读取后续的目录项覆盖至移除位置
                byte[] buffer = new byte[dir.inode.size - writePosition - dirSize];
                Seek(dir, writePosition + dirSize);
                ReadBytes(dir, buffer);
                Seek(dir, writePosition);
                WriteBytes(dir, buffer);

                // 更新目录文件扇区和Inode
                cutSectors.ForEach(sector => m_SectorManager.ClearSector(sector));
                dir.inode.size -= dirSize;
                m_InodeManager.UpdateInode(dir.inode.number, dir.inode);
            }
            catch (Exception e)
            {
                cutSectors.ForEach(sector => sector.Dispose());
                throw new Exception($"移除目录项失败：\n{e.Message}");
            }
        }
        #endregion
    }
}
