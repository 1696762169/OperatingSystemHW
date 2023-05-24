using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW.Msg
{
    public class StringMsg : SerializeMsg
    {
        public override int MsgID => 1;
        public string data;
        public ConsoleColor color;

        public StringMsg() : this("") { }

        public StringMsg(string data, ConsoleColor color = ConsoleColor.White)
        {
            this.data = data;
            this.color = color;
        }

        public override int GetByteCount_Msg()
        {
            return 4 + Encoding.UTF8.GetByteCount(data) + sizeof(ConsoleColor);
        }

        protected override void ToBytesDetail_Msg()
        {
            WriteByte(data);
            WriteByte((int)color);
        }

        protected override void ReadBytesDetail_Msg()
        {
            data = ReadString();
            color = (ConsoleColor)ReadInt();
        }

        public override string ToString()
        {
            return data;
        }
    }
}