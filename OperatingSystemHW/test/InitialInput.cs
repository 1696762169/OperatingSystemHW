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
            sb.Append("input OperatingSystemHW.exe test.exe\n");
            //sb.Append("output test.exe test.exe\n");
            //sb.Append("input OperatingSystemHW.exe test1.exe\n");
            //sb.Append("output test1.exe test1.exe\n");
            //sb.Append("input OperatingSystemHW.exe test2.exe\n");
            //sb.Append("output test2.exe test2.exe\n");
            sb.Append("end\n");
            return new StringReader(sb.ToString());
        }
    }
}
