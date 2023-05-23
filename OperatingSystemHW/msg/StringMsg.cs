using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class StringMsg : SerializeMsg
{
    public override int MsgID => 1;
    public string data;

    public StringMsg() : this("") { }
    public StringMsg(string data) => this.data = data;

    public override int GetByteCount_Msg()
    {
        return 4 + Encoding.UTF8.GetByteCount(data);
    }

    protected override void ToBytesDetail_Msg()
    {
        WriteByte(data);
    }

    protected override void ReadBytesDetail_Msg()
    {
        data = ReadString();
    }

    public override string ToString()
    {
        return data;
    }
}
