
using OperatingSystemHW.test;

namespace OperatingSystemHW
{
    /// <summary>
    /// 磁盘读写速度测试
    /// </summary>
    internal class DiskTest
    {
        private IDiskManager m_DiskManager;  // 磁盘管理器

        public DiskTest(IDiskManager diskManager)
        {
            m_DiskManager = diskManager;
        }

        public static void TestAll(string filePath, int round)
        {
            Console.WriteLine("内存映射读写速度测试");
            DiskManager dm = new(filePath);
            DiskTest test1 = new(dm);
            test1.Test(round);
            test1.TestRandom(round);
            dm.Dispose();

            Console.WriteLine("文件直接读写速度测试");
            FileReadWrite frw = new(filePath);
            DiskTest test2 = new(frw);
            test2.Test(round);
            test2.TestRandom(round);
            frw.Dispose();
        }

        public void Test(int round = 100)
        {
            // 写入测试
            int timer = Environment.TickCount;
            for (int r = 0; r < round; ++r)
            {
                for (int i = 0; i < DiskManager.TOTAL_SECTOR; i++)
                {
                    byte[] buffer = new byte[DiskManager.SECTOR_SIZE];
                    m_DiskManager.WriteBytes(buffer, i * DiskManager.SECTOR_SIZE);
                }
            }
            
            Console.WriteLine($"顺序写入测试完成，用时：{(Environment.TickCount - timer) / 1000.0f}s");

            // 读取测试
            timer = Environment.TickCount;
            for (int r = 0; r < round; ++r)
            {
                for (int i = 0; i < DiskManager.TOTAL_SECTOR; i++)
                {
                    byte[] buffer = new byte[DiskManager.SECTOR_SIZE];
                    m_DiskManager.ReadBytes(buffer, i * DiskManager.SECTOR_SIZE, DiskManager.SECTOR_SIZE);
                }
            }
            Console.WriteLine($"顺序读取测试完成，用时：{(Environment.TickCount - timer) / 1000.0f}s");
        }

        public void TestRandom(int round = 100)
        {
            Random rand = new();
            // 写入测试
            int timer = Environment.TickCount;
            for (int i = 0; i < DiskManager.TOTAL_SECTOR * round; i++)
            {
                byte[] buffer = new byte[DiskManager.SECTOR_SIZE];
                m_DiskManager.WriteBytes(buffer, rand.Next() % DiskManager.TOTAL_SECTOR * DiskManager.SECTOR_SIZE);
            }
            Console.WriteLine($"随机写入测试完成，用时：{(Environment.TickCount - timer) / 1000.0f}s");

            // 读取测试
            timer = Environment.TickCount;
            for (int i = 0; i < DiskManager.TOTAL_SECTOR * round; i++)
            {
                    byte[] buffer = new byte[DiskManager.SECTOR_SIZE];
                m_DiskManager.ReadBytes(buffer, rand.Next() % DiskManager.TOTAL_SECTOR * DiskManager.SECTOR_SIZE, DiskManager.SECTOR_SIZE);
            }
            Console.WriteLine($"随机读取测试完成，用时：{(Environment.TickCount - timer) / 1000.0f}s");
        }
    }
}