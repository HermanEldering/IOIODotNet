﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOIOLib.MessageFrom.Impl
{
    public class SpiOpenFrom : ISpiOpenFrom
    {
        private int spiNum;

        public SpiOpenFrom(int spiNum)
        {
            // TODO: Complete member initialization
            this.spiNum = spiNum;
        }
    }
}