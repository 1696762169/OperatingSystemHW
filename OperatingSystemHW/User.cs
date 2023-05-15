using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 内存用户信息类
    /// </summary>
    internal class User
    {
        public string Name { get; private set; }     // 用户名
        public string Password { get; private set; } // 密码

        public int UserId { get; private set; }         // 用户标识号
        public int GroupId { get; private set; }         // 用户组标识号

        public int HomeNo { get; private set; }      // 用户信息在外存中的Inode编号
        public string Home { get; private set; }     // 用户主目录

        public int CurrentNo { get; private set; }   // 用户信息在外存中的Inode编号
        public string Current { get; private set; }  // 用户当前目录

        public User(DiskUser diskUser)
        {
            UserId = diskUser.uid;
            GroupId = diskUser.gid;
            HomeNo = diskUser.home.inodeNo;
            CurrentNo = diskUser.current.inodeNo;
            unsafe
            {
                Name = Utility.DecodeString(diskUser.name, DiskUser.NAME_MAX_COUNT);
                Password = Utility.DecodeString(diskUser.password, DiskUser.NAME_MAX_COUNT);

                Home = Utility.DecodeString(diskUser.home.name, DiskUser.NAME_MAX_COUNT);
                Current = Utility.DecodeString(diskUser.current.name, DiskUser.NAME_MAX_COUNT);
            }
        }
    }
}
