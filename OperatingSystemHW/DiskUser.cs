using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 硬盘中存储的用户信息类
    /// </summary>
    internal struct DiskUser
    {

        public const int SUPER_USER_ID = 1; // 超级用户ID
        public const int NAME_MAX_COUNT = DirectoryEntry.NAME_MAX_COUNT;   // 用户名最大长度
        public const int PASSWORD_MAX_COUNT = DirectoryEntry.NAME_MAX_COUNT;   // 密码最大长度

        public const int SIZE = 4 * DirectoryEntry.NAME_MAX_COUNT + 4 * sizeof(int); // 用户信息大小

        public unsafe fixed byte name[NAME_MAX_COUNT];     // 用户名
        public unsafe fixed byte password[PASSWORD_MAX_COUNT]; // 密码

        public int uid;         // 用户标识号
        public int gid;         // 用户组标识号

        public DirectoryEntry home;     // 用户主目录
        public DirectoryEntry current;  // 用户当前目录
    }
}
