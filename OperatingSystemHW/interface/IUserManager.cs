using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 用户信息管理接口
    /// </summary>
    internal interface IUserManager
    {
        public User Current { get; }   // 当前用户
        public void SetCurrent(int index);  // 设置当前用户

        /// <summary>
        /// 获取一个用户信息
        /// </summary>
        public User GetUser(int index);
        /// <summary>
        /// 更新一个用户信息
        /// </summary>
        public void UpdateUser(int index);
        /// <summary>
        /// 设置一个用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="index"></param>
        public void SetUser(User user, int index);
    }
}
