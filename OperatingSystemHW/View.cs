using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 用户交互界面类
    /// </summary>
    internal class View
    {
        private readonly IFileManager m_FileManager;
        private readonly IUserManager m_UserManager;

        public View(IFileManager fileManager)
        {
            m_FileManager = fileManager;
            m_UserManager = fileManager.UserManager;
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
                    Console.Write($"{m_UserManager.Current.Name}`{m_FileManager.GetCurrentPath()}>");
                string? command = Console.ReadLine();
                if (string.IsNullOrEmpty(command))
                    continue;

                string[] split = command.Split(' ');
                string cmd = split[0];
                string[] args = split.Where((str, index) => index > 0 && !string.IsNullOrEmpty(str)).ToArray();

                try
                {
                    switch (cmd)
                    {
                    case "ls":
                        ShowEntries();
                        break;
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
                    case "clear":   // 清理屏幕输出
                        Console.Clear();
                        break;
                    case "quit" or "exit":  // 退出应用
                        return;
                    case "debug":   // 是否开启Debug
                        BlockManager.SectorDebug = args is ["on"];
                        break;
                    case "end": // 初始输入结束
                        Console.SetIn(std);
                        initializing = false;
                        break;
                    default:    // 输入有误
                        throw new ArgumentException($"找不到命令：{cmd}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

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
    }
}
