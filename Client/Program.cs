using System;
using System.Net.Sockets;
using System.Text;
using OperatingSystemHW.Msg;

namespace Client
{
    internal class Program
    {
        private static readonly MsgParser _MsgParser = new();
        private static bool _CanSend = true;
        private static bool _Connected = true;
        private static void Main(string[] args)
        {
            // 创建TCP客户端 此处的IP和端口号为待连接的服务端
            using TcpClient client = new("127.0.0.1", 8109);
            // 创建网络流
            using NetworkStream stream = client.GetStream();

            ThreadPool.QueueUserWorkItem(Receive, stream, true);
            while (_Connected)
            {
                string? command = Console.ReadLine();
                if (!_CanSend || string.IsNullOrEmpty(command))
                {
                    continue;
                }

                // 将命令发送到服务器
                Send(stream, command);
            }

            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        // 向服务器发送消息
        private static void Send(NetworkStream stream, string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            stream.Write(new StringMsg(msg).ToBytes());
        }

        // 接收服务器的消息的线程
        private static void Receive(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            while (stream.CanRead)
            {
                int readCount = stream.Read(buffer);
                if (readCount <= 0)
                    continue;
                foreach (SerializeMsg msg in _MsgParser.ParseMsg(buffer[..readCount]))
                {
                    switch (msg)
                    {
                    case StringMsg str:
                        Console.ForegroundColor = str.color;
                        Console.Write(str.data);
                        Console.ResetColor();
                        _CanSend = true;
                        break;
                    case ClearMsg:
                        Console.Clear();
                        break;
                    case ExitMsg:
                        _Connected = false;
                        return;
                    }
                }
            }
        }
    }
}