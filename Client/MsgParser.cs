using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class MsgParser
{
    // 消息类型字典
    private readonly Dictionary<int, Type> m_TypeDict = new();

    // 消息缓存
    private byte[] m_Buffer = new byte[1024 * 32];
    // 当前在缓存中解析到的位置
    private int m_BufferIndex;
    // 当前在缓存中有信息的下一个位置
    private int m_BufferEnd;
    // 当前缓存中的消息长度
    private int BufferValidLength => m_BufferEnd - m_BufferIndex;

    // 初始化消息类型字典
    public MsgParser()
    {
        AddTypeToDict<StringMsg>();
        AddTypeToDict<ExitMsg>();
    }

    // 处理消息
    public List<SerializeMsg> ParseMsg(byte[] data)
    {
        List<SerializeMsg> ret = new();
        // 利用缓存 处理分包
        CopyToBuffer(data);

        // 循环解析 处理黏包
        while (true)
        {
            // 解析消息头
            int msgId, msgLen;
            if (BufferValidLength >= 8)
            {
                // 解析消息类型
                msgId = BitConverter.ToInt32(m_Buffer, m_BufferIndex);
                if (msgId == 0)
                    return ret;
                if (!m_TypeDict.ContainsKey(msgId))
                    throw new ArgumentException("未知的消息类型：" + msgId);
                m_BufferIndex += 4;

                // 解析消息长度
                msgLen = BitConverter.ToInt32(m_Buffer, m_BufferIndex);
                m_BufferIndex += 4;
            }
            // 无法解析消息头 退出解析
            else
                return ret;

            // 尝试反序列化消息
            if (BufferValidLength >= msgLen)
            {
                if (Activator.CreateInstance(m_TypeDict[msgId]) is not SerializeMsg msg)
                    throw new ArgumentException("收到的消息不是SerializeMsg对象");
                msg.ReadBytes(m_Buffer, m_BufferIndex, msgLen);
                m_BufferIndex += msgLen;
                // 将消息添加到返回值中
                ret.Add(msg);
            }
            // 反序列化失败 下次重新解析消息头
            else
            {
                m_BufferIndex -= 8;
                return ret;
            }
        }
    }

    // 将新消息装入缓存
    protected void CopyToBuffer(byte[] data)
    {
        // 进行扩容
        while (data.Length + BufferValidLength > m_Buffer.Length)
        {
            byte[] temp = new byte[m_Buffer.Length * 2];
            Array.Copy(m_Buffer, m_BufferIndex, temp, 0, BufferValidLength);
            m_BufferEnd = BufferValidLength;
            m_BufferIndex = 0;
            m_Buffer = temp;
        }

        // 将内容挪动回开始位置
        if (m_BufferEnd + data.Length > m_Buffer.Length)
        {
            Array.Copy(m_Buffer, m_BufferIndex, m_Buffer, 0, BufferValidLength);
            m_BufferEnd = BufferValidLength;
            m_BufferIndex = 0;
        }

        // 将新消息复制到缓存中
        Array.Copy(data, 0, m_Buffer, m_BufferEnd, data.Length);
        m_BufferEnd += data.Length;
    }

    // 向消息类型字典中添加一个类型
    protected void AddTypeToDict<T>() where T : SerializeMsg, new()
    {
        m_TypeDict.Add(new T().MsgID, typeof(T));
    }
}