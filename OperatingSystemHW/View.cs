using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using OperatingSystemHW.Msg;

namespace OperatingSystemHW
{
    /// <summary>
    /// 用户交互界面类
    /// </summary>
    internal class View
    {
        public User User { get; }

        private readonly ISuperBlockManager m_SuperBlockManager;
        private readonly IUserManager m_UserManager;
        private readonly IFileManager m_FileManager;

        private readonly MsgParser m_MsgParser = new();

        private bool m_Exit;    // 是否退出
        private bool m_EndInitializing; // 是否结束初始输入
        private readonly byte[] m_Buffer = new byte[4096];  // 读写数组

        public View(ISuperBlockManager superBlockManager, IUserManager userManager, IFileManager fileManager)
        {
            m_SuperBlockManager = superBlockManager;
            m_UserManager = userManager;
            User = userManager.GetUser(DiskUser.SUPER_USER_ID).Copy();
            m_FileManager = fileManager;
            m_FileManager.CurrentUser = User;
        }

        /// <summary>
        /// 执行系统的用户交互界面
        /// </summary>
        /// <param name="initial">初始自动执行的内容</param>
        /// <param name="silenceInit">是否在自动执行内容时不显示字符</param>
        public void Start(TextReader? initial = null, bool silenceInit = true)
        {
            TextReader std = Console.In;
            Console.SetIn(initial ?? Console.In);
            bool initializing = initial != null;
            while (true)
            {
                if (!initializing || !silenceInit)
                    Console.Write($"{User.Name}`{m_FileManager.GetCurrentPath()}>");
                string? command = Console.ReadLine();

                try
                {
                    ProcessCommand(command);
                    if (m_Exit)
                        return;
                    if (m_EndInitializing)
                    {
                        m_EndInitializing = false;
                        Console.SetIn(std);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// 执行系统的用户交互界面
        /// </summary>
        /// <param name="client">连接到此服务器的客户端</param>
        public void Start(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            Send(stream, $"连接服务器成功\n{User.Name}`{m_FileManager.GetCurrentPath()}>");
            byte[] buffer = new byte[1024];
            m_Exit = false;
            try
            {
                while (!m_Exit)
                {
                    int readCount = stream.Read(buffer);
                    if (readCount <= 0)
                        continue;
                    foreach (SerializeMsg msg in m_MsgParser.ParseMsg(buffer[..readCount]))
                    {
                        switch (msg)
                        {
                        case StringMsg str:
                            try
                            {
                                // 发送消息
                                ProcessCommand(str.data, stream);
                                if (m_Exit)
                                {
                                    Send(stream, "成功退出服务器，连接已断开\n");
                                    stream.Write(new ExitMsg().ToBytes());
                                }
                            }
                            catch (UnauthorizedAccessException e)
                            {
                                // 发送占用错误
                                Send(stream, e.Message + "\n", ConsoleColor.Yellow);
                            }
                            catch (Exception e)
                            {
                                // 发送错误
                                Send(stream, e.Message + "\n", ConsoleColor.DarkRed);
                            }
                            Send(stream, $"{User.Name}`{m_FileManager.GetCurrentPath()}>");
                            break;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            finally
            {
                // 退出时关闭所有打开的文件
                foreach (OpenFile file in User.OpenFiles.Values)
                    m_FileManager.Close(file);
                User.OpenFiles.Clear();
            }
        }

        // 处理指令
        private void ProcessCommand(string? command, NetworkStream? stream = null)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string[] split = command.Split(' ');
            string cmd = split[0];
            string[] args = split.Where((str, index) => index > 0 && !string.IsNullOrEmpty(str)).ToArray();

            if (stream == null)
                ProcessCommand(cmd, args);
            else
                ProcessCommand(cmd, args, stream);
        }

        // 网络模式处理返回结果的指令
        private void ProcessCommand(string cmd, string[] args, NetworkStream stream)
        {
            switch (cmd)
            {
            case "ls":
                ShowEntries(stream);
                break;
            case "clear":
                stream.Write(new ClearMsg().ToBytes());
                break;
            case "cat":
                ShowFile(args, stream);
                break;
            case "stat":
                ShowFileState(args, stream);
                break;
            default:
                ProcessSilenceCommand(cmd, args);
                break;
            }
        }
        // 控制台模式处理返回结果的指令
        private void ProcessCommand(string cmd, string[] args)
        {
            switch (cmd)
            {
            case "ls":
                ShowEntries();
                break;
            case "clear":
                Console.Clear();
                break;
            default:
                ProcessSilenceCommand(cmd, args);
                break;
            }
        }

        // 处理无需返回结果的指令
        private void ProcessSilenceCommand(string cmd, string[] args)
        {
            switch (cmd)
            {
            case "touch":
                CreateFile(args);
                break;
            case "rm":
                DeleteFile(args);
                break;
            case "mkdir":
                CreateDirectory(args);
                break;
            case "rmdir":
                DeleteDirectory(args);
                break;
            case "cd":
                ChangeDirectory(args);
                break;
            case "input":
                MoveFileIn(args);
                break;
            case "output":
                MoveFileIOut(args);
                break;
            case "open":
                OpenFile(args);
                break;
            case "close":
                CloseFile(args);
                break;
            case "quit" or "exit":  // 退出应用
                m_Exit = true;
                break;
            case "debug":   // 是否开启Debug
                BlockManager.SectorDebug = args is ["on"];
                break;
            case "end": // 初始输入结束
                m_EndInitializing = true;
                break;
            default:    // 输入有误
                throw new ArgumentException($"找不到命令：{cmd}");
            }
        }

        #region 指令处理函数
        // 列出当前目录下的文件
        private void ShowEntries()
        {
            foreach (Entry entry in m_FileManager.GetEntries())
            {
                Console.ForegroundColor = PathUtility.IsDirectory(entry.name) ? ConsoleColor.Cyan : ConsoleColor.White;
                Console.WriteLine(entry.name);
            }
            Console.ResetColor();
        }
        private void ShowEntries(NetworkStream stream)
        {
            foreach (Entry entry in m_FileManager.GetEntries())
            {
                ConsoleColor color = PathUtility.IsDirectory(entry.name) ? ConsoleColor.Cyan : ConsoleColor.White;
                Send(stream, entry.name + "\n", color);
            }
        }

        // 创建文件
        private void CreateFile(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            m_FileManager.CreateFile(args[0]);
        }
        // 删除文件
        private void DeleteFile(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            m_FileManager.DeleteFile(args[0]);
        }

        // 创建文件夹
        private void CreateDirectory(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            m_FileManager.CreateDirectory(args[0]);
        }
        // 删除文件夹
        private void DeleteDirectory(IReadOnlyList<string> args)
        {
            if (args.Count != 1 && args.Count != 2)
                throw new ArgumentException($"参数数量错误，应为 1/2 个参数，实际得到 {args.Count} 个");
            m_FileManager.DeleteDirectory(args[0], args is [_, "-r"]);
        }

        // 更改当前工作文件夹
        private void ChangeDirectory(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            m_FileManager.ChangeDirectory(args[0]);
            m_UserManager.SetUser(User, User.UserId);
        }

        // 将文件移动到二级文件系统中
        private void MoveFileIn(IReadOnlyList<string> args)
        {
            if (args.Count != 2)
                throw new ArgumentException($"参数数量错误，应为 2 个参数，实际得到 {args.Count} 个");

            // 检查文件和目录是否存在
            if (!File.Exists(args[0]))
                throw new FileNotFoundException($"未找到待移入文件：{args[0]}");

            // 如果文件存在则先删除文件 然后创建新文件
            if (m_FileManager.FileExists(args[1]))
                m_FileManager.DeleteFile(args[1]);
            m_FileManager.CreateFile(args[1]);

            // 打开文件进行写入
            using OpenFile file = m_FileManager.Open(args[1]);
            m_FileManager.WriteBytes(file, File.ReadAllBytes(args[0]));
        }
        // 将二级文件系统中的文件移出
        private void MoveFileIOut(IReadOnlyList<string> args)
        {
            if (args.Count != 2)
                throw new ArgumentException($"参数数量错误，应为 2 个参数，实际得到 {args.Count} 个");

            // 检查文件和目录是否存在
            if (!m_FileManager.FileExists(args[0]))
                throw new FileNotFoundException($"未找到待移出文件：{args[0]}");

            // 如果文件不存在则创建文件
            if (!File.Exists(args[1]))
                File.Create(args[1]).Close();

            // 打开文件读出内容
            using OpenFile file = m_FileManager.Open(args[0]);
            byte[] buffer = new byte[file.inode.size];
            m_FileManager.ReadBytes(file, buffer);
            File.WriteAllBytes(args[1], buffer);
        }

        // 通过网络方式预览文本文件内容
        private void ShowFile(IReadOnlyList<string> args, NetworkStream stream)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            using OpenFile file = m_FileManager.Open(args[0]);
            if (file.inode.size == 0)
                throw new Exception("文件为空");
            m_FileManager.ReadBytes(file, m_Buffer, file.inode.size);
            Send(stream, Encoding.UTF8.GetString(m_Buffer, 0, Math.Min(file.inode.size, m_Buffer.Length)) + "\n");
            if (file.inode.size > m_Buffer.Length)
                Send(stream, $"-----文件过大，仅展示前{m_Buffer.Length}个字节的内容-----\n", ConsoleColor.Yellow);

        }
        // 通过网络方式查看文件Inode信息
        private void ShowFileState(IReadOnlyList<string> args, NetworkStream stream)
        {
            if (args.Count != 1 && args.Count != 0)
                throw new ArgumentException($"参数数量错误，应为 0/1 个参数，实际得到 {args.Count} 个");
            if (args.Count != 1)
            {
                Send(stream, $"剩余空间大小：{m_SuperBlockManager.Sb.FreeCount * DiskManager.SECTOR_SIZE / 1024.0f:F1}KB\n" +
                             $"剩余数据扇区数量：{m_SuperBlockManager.Sb.FreeCount}\n" +
                             $"剩余空间比例：{(float)m_SuperBlockManager.Sb.FreeCount / m_SuperBlockManager.Sb.DataSector:P}\n" +
                             $"剩余Inode数量：{m_SuperBlockManager.Sb.FreeInode}\n");
            }
            else
            {
                using OpenFile file = m_FileManager.Open(args[0], false);
                Send(stream, $"文件名：{args[0]}\n" +
                             $"文件大小：{file.inode.size,-12} 文件Inode编号：{file.inode.number}\n" +
                             $"最后访问时间：{Utility.ToTime(file.inode.accessTime)}\n" +
                             $"最后修改时间：{Utility.ToTime(file.inode.modifyTime)}\n");
            }
        }

        // 打开文件
        private void OpenFile(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            int inodeNo = m_FileManager.GetFileInode(args[0]);
            if (!User.OpenFiles.ContainsKey(inodeNo) || User.OpenFiles[inodeNo].Disposed)
                User.OpenFiles[inodeNo] = m_FileManager.Open(args[0]);
        }
        // 关闭文件
        private void CloseFile(IReadOnlyList<string> args)
        {
            if (args.Count != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Count} 个");
            int inodeNo = m_FileManager.GetFileInode(args[0]);
            if (!User.OpenFiles.TryGetValue(inodeNo, out OpenFile? file))
                return;
            m_FileManager.Close(file);
            User.OpenFiles.Remove(inodeNo);
        }
        #endregion

        // 向客户端发送消息
        private static void Send(NetworkStream stream, string msg, ConsoleColor color = ConsoleColor.White)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            stream.Write(new StringMsg(msg, color).ToBytes());
        }
    }
}
