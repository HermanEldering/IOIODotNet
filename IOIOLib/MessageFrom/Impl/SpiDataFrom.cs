﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOIOLib.MessageFrom.Impl
{
    public class SpiDataFrom : ISpiDataFrom
    {
        private int spiNum;
        private int ssPin;
        private byte[] data;
        private int dataBytes;

        public SpiDataFrom(int spiNum, int ssPin, byte[] data, int dataBytes)
        {
            // TODO: Complete member initialization
            this.spiNum = spiNum;
            this.ssPin = ssPin;
            this.data = data;
            this.dataBytes = dataBytes;
        }
    }
}