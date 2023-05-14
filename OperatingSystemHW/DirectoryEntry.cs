﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// 文件目录项
    /// </summary>
    internal struct DirectoryEntry
    {
        // 遵循原著 就定28吧
        public const int NAME_MAX_COUNT = 28;

        public int inodeNo;         // 文件Inode编号
        public unsafe fixed byte name[NAME_MAX_COUNT];         // 文件名
    }
}
