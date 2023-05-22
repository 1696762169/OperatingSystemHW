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

        public View(IUserManager userManager, IFileManager fileManager)
        {
            m_FileManager = fileManager;
            m_UserManager = userManager;
        }

        /// <summary>
        /// 执行系统的用户交互界面
        /// </summary>
        /// <param name="initial">初始自动执行的内容</param>
        /// <param name="silenceInit">是否在自动执行内容时不显示字符</param>
        public void Start(TextReader? initial = null, bool silenceInit = false)
        {
            Console.SetIn(initial ?? Console.In);
            while (true)
            {
                if (!silenceInit)
                    Console.Write($"{m_UserManager.Current.Name}`{m_UserManager.Current.Current}>");
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
                    case "clear":   // 清理屏幕输出
                        Console.Clear();
                        break;
                    case "quit" or "exit":  // 退出应用
                        return;
                    case "end": // 初始输入结束
                        Console.SetIn(Console.In);
                        silenceInit = false;
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
        private void CreateFile(string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Length} 个");
            m_FileManager.CreateFile(args[0]);
        }

        // 删除文件
        private void DeleteFile(string[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException($"参数数量错误，应为 1 个参数，实际得到 {args.Length} 个");
            m_FileManager.DeleteFile(args[0]);
        }
    }
}
