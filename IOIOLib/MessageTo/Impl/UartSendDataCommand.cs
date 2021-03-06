﻿/*
 * Copyright 2011 Ytai Ben-Tsvi. All rights reserved.
 * Copyright 2015 Joe Freeman. All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 * 
 *    1. Redistributions of source code must retain the above copyright notice, this list of
 *       conditions and the following disclaimer.
 * 
 *    2. Redistributions in binary form must reproduce the above copyright notice, this list
 *       of conditions and the following disclaimer in the documentation and/or other materials
 *       provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED 'AS IS AND ANY EXPRESS OR IMPLIED
 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL ARSHAN POURSOHI OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 * ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied.
 */

using IOIOLib.Device.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOIOLib.Device;
using IOIOLib.Component.Types;
using IOIOLib.Util;
using IOIOLib.Message.Impl;

namespace IOIOLib.MessageTo.Impl
{
    public class UartSendDataCommand : IOIOMessageNotification<IUartSendDataCommand>,IUartSendDataCommand
    {
		private static IOIOLog LOG = IOIOLogManager.GetLogger(typeof(UartCloseCommand));

		public UartSpec UartDef { get; private set; }

		public byte[] Data { get; private set; }


        /// <summary>
        /// The IOIO supports up to 64 bytes per message
        /// </summary>
        /// <param name="uart"></param>
        /// <param name="data"></param>
        /// <param name="size"></param>
        internal UartSendDataCommand(UartSpec uart, byte[] data)
        {
            this.UartDef = uart;
			this.Data = data;
        }

        public bool ExecuteMessage(Device.Impl.IOIOProtocolOutgoing outBound)
        {
			outBound.uartData(UartDef.UartNumber, Data.Length, Data);
			return true;
		}


		public bool Alloc(IResourceManager rManager)
		{
			return true;
		}

        public int PayloadSize()
        {
            // does not include the uart number which is used to determine which buffer payload goes in
            // size + the data
            return this.Data.Length;
        }

        public override string ToString()
        {
            return this.GetType().Name + " Uart:" + this.UartDef.UartNumber+ " Size:"+this.PayloadSize();
        }

    }
}
