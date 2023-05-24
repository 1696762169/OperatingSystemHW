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

        private readonly IUserManager m_UserManager;
        private readonly IFileManager m_FileManager;

        private readonly MsgParser m_MsgParser = new();

        private bool m_Exit;
        private bool m_EndInitializing;

        public View(IUserManager userManager, IFileManager fileManager)
        {
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
            Send(stream, $"{User.Name}`{m_FileManager.GetCurrentPath()}>");
            byte[] buffer = new byte[1024];
            m_Exit = false;
            while (!m_Exit)
            {
                int readCount;
                try
                {
                    readCount = stream.Read(buffer);
                }
                catch (IOException)
                {
                    return;
                }
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
                                return;
                            }
                            Send(stream,$"{User.Name}`{m_FileManager.GetCurrentPath()}>");
                        }
                        catch (Exception e)
                        {
                            // 发送错误
                            Send(stream, e.Message + "\n");
                            Send(stream, $"{User.Name}`{m_FileManager.GetCurrentPath()}>");
                        }
                        break;
                    }
                }
            }
        }

        // 处理指令
        private void ProcessCommand(string? command, NetworkStream? stream = null)
        {
            if (string.IsNullOrEmpty(command))
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
            OpenFile file = m_FileManager.Open(args[1]);
            m_FileManager.WriteBytes(file, File.ReadAllBytes(args[0]));
            m_FileManager.Close(file);
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
            OpenFile file = m_FileManager.Open(args[0]);
            byte[] buffer = new byte[file.inode.size];
            m_FileManager.ReadBytes(file, buffer);
            m_FileManager.Close(file);
            File.WriteAllBytes(args[1], buffer);
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
