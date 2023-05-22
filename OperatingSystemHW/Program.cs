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

            User super = fileSystem.GetUser(0);

            foreach (Entry entry in fileManager.GetEntries())
            {
                System.Console.WriteLine(entry.name);
            }
            fileManager.CreateFile("/a.txt");
            //fileManager.CreateFile("/b.txt");
            //fileManager.CreateFile("/c.txt");
            foreach (Entry entry in fileManager.GetEntries())
            {
                System.Console.WriteLine(entry.name);
            }
            fileManager.DeleteFile("/a.txt");
            foreach (Entry entry in fileManager.GetEntries())
            {
                System.Console.WriteLine(entry.name);
            }
        }
    }
}