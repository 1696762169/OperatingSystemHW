using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 用户信息类
    /// </summary>
    internal class User
    {
        public string name;     // 用户名
        public string password; // 密码
        public int uid;         // 用户标识号
        public int gid;         // 用户组标识号

        public string home;     // 用户主目录
        public string current;  // 用户当前目录
    }
}
