using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.test
{
    /// <summary>
    /// 模拟程序的初始输入
    /// </summary>
    internal static class InitialInput
    {
        /// <summary>
        /// 大文件输入输出测试
        /// </summary>
        /// <returns></returns>
        public static StringReader BigFileInOut()
        {
            StringBuilder sb = new();
            sb.AppendLine("input OperatingSystemHW.exe test.exe");
            //sb.AppendLine("output test.exe test.exe");
            //sb.AppendLine("input OperatingSystemHW.exe test1.exe");
            //sb.AppendLine("output test1.exe test1.exe");
            //sb.AppendLine("input OperatingSystemHW.exe test2.exe");
            //sb.AppendLine("output test2.exe test2.exe");
            //sb.AppendLine("end");
            return new StringReader(sb.ToString());
        }

        /// <summary>
        /// 文件创建测试
        /// </summary>
        /// <returns></returns>
        public static StringReader CreateFile()
        {
            StringBuilder sb = new();
            Random rand = new();

            for (int i = 0; i < 20; ++i)
            {
                if (rand.Next() % 3 == 0)
                    sb.AppendLine($"mkdir dir{rand.Next(i * 100, (i + 1) * 100)}{(rand.Next() % 2 == 0 ? "/" : "")}");
                else
                    sb.AppendLine($"touch file{rand.Next(i * 100, (i + 1) * 100)}");
            }

            for (int i = 0; i < 5; ++i)
            {
                sb.AppendLine($"mkdir in{i}");
                sb.AppendLine($"cd in{i}");
            }

            for (int i = 0; i < 20; ++i)
            {
                if (rand.Next() % 3 == 0)
                    sb.AppendLine($"mkdir dir{rand.Next(i * 100, (i + 1) * 100)}{(rand.Next() % 2 == 0 ? "/" : "")}");
                else
                    sb.AppendLine($"touch file{rand.Next(i * 100, (i + 1) * 100)}");
            }

            sb.AppendLine("cd /");
            sb.AppendLine("end");
            return new StringReader(sb.ToString());
        }
    }
}
