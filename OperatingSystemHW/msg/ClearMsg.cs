using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.Msg
{
    /// <summary>
    /// 清空屏幕消息
    /// </summary>
    public class ClearMsg : SerializeMsg
    {
        public override int MsgID => 2;
        public override int GetByteCount_Msg() => 0;

        protected override void ToBytesDetail_Msg() { }

        protected override void ReadBytesDetail_Msg() { }
    }
}
