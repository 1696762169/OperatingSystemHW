using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OperatingSystemHW.Msg
{
    public abstract class SerializeData
    {
        /// <summary>
        /// 获取此对象转换为byte数组的长度
        /// </summary>
        public abstract int GetByteCount();
        /// <summary>
        /// 获取此对象转换为的byte数组
        /// </summary>
        public byte[] ToBytes()
        {
            binaryData = new byte[GetByteCount()];
            binaryIndex = 0;
            ToBytesDetail();
            return binaryData;
        }
        // 将此对象数据序列化到binaryData
        protected abstract void ToBytesDetail();

        /// <summary>
        /// 将传入的byte数组中的内容反序列化到此对象中
        /// </summary>
        /// <param name="bits"></param>
        public void ReadBytes(byte[] bits)
        {
            binaryData = bits;
            binaryIndex = 0;
            ReadBytesDetail();
        }
        public void ReadBytes(byte[] bits, int index, int count = -1)
        {
            if (count == -1)
                count = bits.Length - index;
            binaryData = new byte[count];
            Array.Copy(bits, index, binaryData, 0, count);
            binaryIndex = 0;
            ReadBytesDetail();
        }
        // 将binaryData中的数据反序列化到此对象中
        protected abstract void ReadBytesDetail();

        // 序列化后得到的字节数组
        protected byte[] binaryData = Array.Empty<byte>();
        // 序列化/反序列化时使用的序号
        protected int binaryIndex;

        #region 写入字节
        /* 写入简单类型 */
        protected void WriteByte(int value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 4;
        }
        protected void WriteByte(long value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 8;
        }
        protected void WriteByte(uint value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 4;
        }
        protected void WriteByte(ulong value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 8;
        }
        protected void WriteByte(short value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 2;
        }
        protected void WriteByte(ushort value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 2;
        }
        protected void WriteByte(float value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 4;
        }
        protected void WriteByte(double value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 8;
        }
        protected void WriteByte(bool value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 1;
        }
        protected void WriteByte(char value)
        {
            BitConverter.GetBytes(value).CopyTo(binaryData, binaryIndex);
            binaryIndex += 1;
        }

        /* 写入string */
        protected void WriteByte(string value)
        {
            byte[] temp = Encoding.UTF8.GetBytes(value);
            WriteByte(temp.Length);
            temp.CopyTo(binaryData, binaryIndex);
            binaryIndex += temp.Length;
        }

        /* 写入自定义类型 */
        protected void WriteByte(SerializeData value)
        {
            value.ToBytes().CopyTo(binaryData, binaryIndex);
            binaryIndex += value.GetByteCount();
        }
        #endregion

        #region 读取字节
        /* 读取简单类型 */
        protected int ReadInt()
        {
            int ret = BitConverter.ToInt32(binaryData, binaryIndex);
            binaryIndex += 4;
            return ret;
        }
        protected long ReadLong()
        {
            long ret = BitConverter.ToInt64(binaryData, binaryIndex);
            binaryIndex += 8;
            return ret;
        }
        protected uint ReadUint()
        {
            uint ret = BitConverter.ToUInt32(binaryData, binaryIndex);
            binaryIndex += 4;
            return ret;
        }
        protected ulong ReadUlong()
        {
            ulong ret = BitConverter.ToUInt64(binaryData, binaryIndex);
            binaryIndex += 8;
            return ret;
        }
        protected short ReadShort()
        {
            short ret = BitConverter.ToInt16(binaryData, binaryIndex);
            binaryIndex += 2;
            return ret;
        }
        protected ushort ReadUshort()
        {
            ushort ret = BitConverter.ToUInt16(binaryData, binaryIndex);
            binaryIndex += 2;
            return ret;
        }
        protected float ReadFloat()
        {
            float ret = BitConverter.ToSingle(binaryData, binaryIndex);
            binaryIndex += 4;
            return ret;
        }
        protected double ReadDouble()
        {
            double ret = BitConverter.ToDouble(binaryData, binaryIndex);
            binaryIndex += 8;
            return ret;
        }
        protected bool ReadBoolean()
        {
            bool ret = BitConverter.ToBoolean(binaryData, binaryIndex);
            binaryIndex += 1;
            return ret;
        }
        protected char ReadChar()
        {
            char ret = BitConverter.ToChar(binaryData, binaryIndex);
            binaryIndex += 1;
            return ret;
        }

        /* 读取string */
        protected string ReadString()
        {
            int length = ReadInt();
            string ret = Encoding.UTF8.GetString(binaryData, binaryIndex, length);
            binaryIndex += length;
            return ret;
        }

        /* 读取自定义类型 */
        protected T ReadData<T>() where T : SerializeData, new()
        {
            T ret = new T();
            ret.ReadBytes(binaryData, binaryIndex);
            binaryIndex += ret.GetByteCount();
            return ret;
        }
        #endregion
    }
}