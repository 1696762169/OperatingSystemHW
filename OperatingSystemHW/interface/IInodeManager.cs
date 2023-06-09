﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHW
{
    /// <summary>
    /// Inode管理接口
    /// </summary>
    internal interface IInodeManager
    {
        public bool Formatting { get; set; }
        /// <summary>
        /// 获取一个空闲Inode的控制权
        /// </summary>
        public Inode GetEmptyInode();
        /// <summary>
        /// 获取一个Inode的控制权
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Inode已被占用</exception>
        public Inode GetInode(int inodeNo);
        /// <summary>
        /// 释放一个Inode
        /// </summary>
        public void PutInode(int inodeNo);
        /// <summary>
        /// 更新一个Inode的数据到外存中
        /// </summary>
        public void UpdateInode(int inodeNo, Inode inode);
    }
}
