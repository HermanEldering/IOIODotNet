﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOIOLib.MessageFrom.Impl
{
    public class I2cOpenFrom : II2cOpenFrom
    {
        public int I2cNum { get; private set; }

        public I2cOpenFrom(int i2cNum)
        {
            // TODO: Complete member initialization
            this.I2cNum = i2cNum;
        }
    }
}
