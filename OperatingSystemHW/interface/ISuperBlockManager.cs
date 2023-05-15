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
        public ref SuperBlock Sb { get; }
        /// <summary>
        /// 将超级块中的非用户内容写入文件
        /// </summary>
        public void UpdateSuperBlock();
    }
}
