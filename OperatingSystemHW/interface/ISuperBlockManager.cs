using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 超级块管理接口
    /// </summary>
    internal interface ISuperBlockManager
    {
        public SuperBlock Sb { get; }   // 超级块
        /// <summary>
        /// 将超级块内容写入文件
        /// </summary>
        public void UpdateSuperBlock();
    }
}
