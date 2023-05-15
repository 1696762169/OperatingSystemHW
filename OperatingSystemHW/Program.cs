using System.Runtime.InteropServices;
using OperatingSystemHW.test;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string filePath = "disk.img";
            Console.WriteLine(DiskUser.SIZE);
            DiskManager diskManager = new DiskManager(filePath);
            BlockManager blockManager = new BlockManager(diskManager);

            diskManager.Dispose();
        }
    }
}