using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.Msg
{
    /// <summary>
    /// 断开连接消息
    /// </summary>
    public class ExitMsg : SerializeMsg
    {
        public override int MsgID => int.MinValue;

        public override int GetByteCount_Msg() => 0;

        protected override void ToBytesDetail_Msg() { }

        protected override void ReadBytesDetail_Msg() { }
    }
}
