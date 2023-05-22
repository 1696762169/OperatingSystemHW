using System.Runtime.InteropServices;
using OperatingSystemHW.test;
using System.Linq;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string filePath = "disk.img";

            using DiskManager diskManager = new(filePath);
            //using DiskManager diskManager = new(filePath, true);
            FileSystem fileSystem = new(diskManager);
            BlockManager blockManager = new(diskManager, fileSystem);
            FileManager fileManager = new(blockManager, blockManager, fileSystem);

            View view = new(fileManager);
            view.Start();
        }
    }
}