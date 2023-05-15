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

        /// <summary>
        /// 获取超级块中的签名
        /// </summary>
        public string GetSignature();
        /// <summary>
        /// 设置超级块签名
        /// </summary>
        public void SetSignature(string signature);
    }
}
