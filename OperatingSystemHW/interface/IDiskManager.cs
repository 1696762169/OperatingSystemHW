using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    internal interface IDiskManager
    {
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="buffer"></param>
        public void ReadBytes(byte[] buffer, int offset, int count);
        /// <summary>
        /// 写入字节
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="buffer"></param>
        public void WriteBytes(byte[] buffer, int offset);

        /// <summary>
        /// 在指定位置读取结构体
        /// </summary>
        public void Read<T>(int position, out T value) where T : unmanaged;
        /// <summary>
        /// 在指定位置写入结构体
        /// </summary>
        public void Write<T>(int position, ref T value) where T : unmanaged;

        /// <summary>
        /// 读取数组
        /// </summary>
        /// <param name="position">读取起始位置</param>
        /// <param name="array">数据存储数组</param>
        /// <param name="offset">数组序号偏移量</param>
        /// <param name="count">读取元素数量</param>
        public void ReadArray<T>(int position, T[] array, int offset, int count) where T : unmanaged;
        /// <summary>
        /// 写入数组
        /// </summary>
        /// <param name="position">读取起始位置</param>
        /// <param name="array">数据存储数组</param>
        /// <param name="offset">数组序号偏移量</param>
        /// <param name="count">写入元素数量</param>
        public void WriteArray<T>(int position, T[] array, int offset, int count) where T : unmanaged;
    }
}
