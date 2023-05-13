using System.Runtime.InteropServices;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            DiskManager dm = new("disk.img", true);
            for (int i = 0; i < 1024 / 4; i++)
            {
                dm.accessor.Read(i * sizeof(int), out int data);
                Console.WriteLine($"第{i + 1}个整数值：{data}");
            }

            dm.Dispose();
        }
    }
}