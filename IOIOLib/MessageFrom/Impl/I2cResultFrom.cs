﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOIOLib.MessageFrom.Impl
{
    public class I2cResultFrom : II2cResultFrom
    {
        private int i2cNum;
        private int size;
        private byte[] data;

        public I2cResultFrom(int i2cNum, int size, byte[] data)
        {
            // TODO: Complete member initialization
            this.i2cNum = i2cNum;
            this.size = size;
            this.data = data;
        }
    }
}