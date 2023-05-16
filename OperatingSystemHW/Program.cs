using System.Runtime.InteropServices;
using OperatingSystemHW.test;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string filePath = "disk.img";

            DiskManager diskManager = new(filePath);
            FileSystem fileSystem = new(diskManager);
            BlockManager blockManager = new(diskManager, fileSystem);
            FileManager fileManager = new(blockManager, blockManager, fileSystem);

            User super = fileSystem.GetUser(0);
            diskManager.Dispose();
        }
    }
}