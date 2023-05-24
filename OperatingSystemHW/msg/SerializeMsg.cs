using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.Msg
{
    public abstract class SerializeMsg : SerializeData
    {
        public abstract int MsgID { get; }
        public override int GetByteCount()
        {
            return 4    // 消息类型ID长度
                + 4     // 消息长度变量长度
                + GetByteCount_Msg();    // 实际的反序列化长度
        }
        // 子类实际要实现的获取消息长度的方法
        public abstract int GetByteCount_Msg();

        protected override void ToBytesDetail()
        {
            WriteByte(MsgID);
            WriteByte(binaryData.Length - 8);   // 此处binaryData的长度已经被设定为GetByteCount的返回值
            ToBytesDetail_Msg();
        }
        // 子类实际要实现的序列化方法
        protected abstract void ToBytesDetail_Msg();

        protected override void ReadBytesDetail() => ReadBytesDetail_Msg();
        // 子类实际要实现的反序列化方法
        protected abstract void ReadBytesDetail_Msg();
    }
}