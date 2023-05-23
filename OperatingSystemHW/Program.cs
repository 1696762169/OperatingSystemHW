using System.Diagnostics;
using System.Runtime.InteropServices;
using OperatingSystemHW.test;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            //Console.InputEncoding = Encoding.Default;
            //Console.OutputEncoding = Encoding.Default;

            const string FILE_PATH = "disk.img";

            using DiskManager diskManager = new(FILE_PATH);
            //using DiskManager diskManager = new(FILE_PATH, true);
            FileSystem fileSystem = new(diskManager);
            BlockManager blockManager = new(diskManager, fileSystem);
            //FileManager fileManager = new(blockManager, blockManager, fileSystem);
            //View view = new(fileManager);

            //view.Start();
            //view.Start(InitialInput.BigFileInOut());
            //view.Start(InitialInput.CreateFile());

            // 创建新的TcpListener并开始监听
            TcpListener listener = new(IPAddress.Parse("127.0.0.1"), 8109);
            listener.Start();

            // 持续监听请求
            while (true)
            {
                // 当有请求时接受连接
                TcpClient tcpClient = listener.AcceptTcpClient();
                Console.WriteLine($"成功连接到客户端（{tcpClient.Client.RemoteEndPoint}）");

                ThreadPool.QueueUserWorkItem((client) =>
                {
                    FileManager fileManager = new(blockManager, blockManager, fileSystem);
                    View view = new(fileManager);
                    view.Start(client);

                    Console.WriteLine($"客户端断开连接（{tcpClient.Client.RemoteEndPoint}）");

                    // 断开连接
                    client.Close();
                }, tcpClient, true);
            }
        }
    }
}