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
        /// <summary>
        /// 获取一个用户信息
        /// </summary>
        public User GetUser(int uid);
        /// <summary>
        /// 更新一个用户信息
        /// </summary>
        public void UpdateUser(int uid);
        /// <summary>
        /// 设置一个用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <param name="uid"></param>
        public void SetUser(User user, int uid);
    }
}
