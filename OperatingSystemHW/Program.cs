using System.Runtime.InteropServices;
using OperatingSystemHW.test;
using System.Linq;
using System.Text;

namespace OperatingSystemHW
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            const string FILE_PATH = "disk.img";

            using DiskManager diskManager = new(FILE_PATH);
            //using DiskManager diskManager = new(FILE_PATH, true);
            FileSystem fileSystem = new(diskManager);
            BlockManager blockManager = new(diskManager, fileSystem);
            FileManager fileManager = new(blockManager, blockManager, fileSystem);

            View view = new(fileManager);
            view.Start();
            //view.Start(InitialInput.BigFileInOut());
        }
    }
}