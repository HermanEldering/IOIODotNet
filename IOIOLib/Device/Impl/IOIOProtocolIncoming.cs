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
using IOIOLib.IOIOException;
using IOIOLib.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IOIOLib.Device.Impl
{
    /// <summary>
    /// Handles all incoming messages for a given single device (connection) in its own thread.
    /// This lets us capture incoming data without polling or blocking the main thread
    /// </summary>
    public class IOIOProtocolIncoming
    {
        private static IOIOLog LOG = IOIOLogManager.GetLogger(typeof(IOIOProtocolIncoming));

        private List<int> AnalogPinValues_ = new List<int>();
        private List<int> AnalogFramePins_ = new List<int>();
        private List<int> NewFramePins_ = new List<int>();
        // use type HashSet because it implements RemoveWhere
        private HashSet<int> RemovedPins_ = new HashSet<int>();
        private HashSet<int> AddedPins_ = new HashSet<int>();
        private Stream Stream_;
        /// <summary>
        /// Could be a handler distributor with multiple other handlers in it
        /// </summary>
        private IOIOIncomingHandler Handler_;
        /// <summary>
        /// this should go somewhere else
        /// </summary
        internal CancellationTokenSource CancelTokenSource_;
        /// <summary>
        /// Why do we retain reference to this when we have cancel token access?
        /// </summary>
        private Task IncomingTask_;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Stream">incoming Stream from the IOIO</param>
        /// <param name="handler">handler or handler distributor that will added to the notification list</param>
        /// <param name="cancelTokenSource">A cancellation token so that this will be stopped with other threads. Creates a token if none passed in</param>
        public IOIOProtocolIncoming(Stream stream, IOIOIncomingHandler handler, CancellationTokenSource cancelTokenSource)
        {
            this.Stream_ = stream;
            this.Handler_ = handler;
            if (cancelTokenSource != null)
            {
                CancelTokenSource_ = cancelTokenSource;
            }
            else
            {
                CancelTokenSource_ = new CancellationTokenSource();
            }
            IncomingTask_ = new Task(run, CancelTokenSource_.Token, TaskCreationOptions.LongRunning);
            IncomingTask_.Start();
        }

        /// <summary>
        /// This object will generate its own cancellation token
        /// </summary>
        /// <param name="Stream"></param>
        /// <param name="handler"></param>
        public IOIOProtocolIncoming(Stream stream, IOIOIncomingHandler handler) :
            this(stream, handler, new CancellationTokenSource())
        {
            // constructor exists only for constructor chaining   
        }
        private void calculateAnalogFrameDelta()
        {
            // would have tried .Except() here but that returns IEnumerable instead of a list or set
            RemovedPins_.Clear();
            RemovedPins_.Union(AnalogFramePins_);
            AddedPins_.Clear();
            AddedPins_.Union(NewFramePins_);
            // Remove the intersection from both.
            List<int> toRemove = new List<int>();
            foreach (int onePin in RemovedPins_)
            {
                if (AddedPins_.Contains(onePin))
                {
                    toRemove.Add(onePin);
                }
            }
            RemovedPins_.RemoveWhere(x => toRemove.Contains(x));
            AddedPins_.RemoveWhere(x => toRemove.Contains(x));
            // swap
            List<int> temp = AnalogFramePins_;
            AnalogFramePins_ = NewFramePins_;
            NewFramePins_ = temp;
        }

        private int readByte()
        {
            try
            {
                while (true)
                {
                    CancelTokenSource_.Token.ThrowIfCancellationRequested();
                    try
                    {
                        int b = Stream_.ReadByte();
                        if (b < 0)
                        {
                            throw new IOException("Unexpected Stream_ closure");
                        }

                        LOG.Debug(IncomingTask_.Id + " received 0x" + b.ToString("X"));
                        return b;
                    }
                    catch (TimeoutException e)
                    {
                        LOG.Debug(IncomingTask_.Id + " readByte " + e.Message + " retrying");
                    }
                }
            }
            catch (System.Threading.ThreadAbortException e)
            {
                LOG.Warn(IncomingTask_.Id + " Thread aborted while in read ", e);
                throw e;
            }
            catch (IOException e)
            {
                LOG.Warn(IncomingTask_.Id + " IOIO disconnected while in read");
                throw e;
            }
        }

        private void readBytes(int size, byte[] buffer)
        {
            for (int i = 0; i < size; ++i)
            {
                buffer[i] = (byte)readByte();
            }
        }

        public void run()
        {
            // should set to highest priority
            int arg1;
            int arg2;
            int numPins;
            int size;
            byte[] data = new byte[256];
            try
            {
                // this was while(true) in the java code
                while (true)
                {
                    CancelTokenSource_.Token.ThrowIfCancellationRequested();
                    arg1 = readByte();
                    LOG.Debug(IncomingTask_.Id + " Processing reply type " + arg1.ToString("X"));
                    switch (arg1)
                    {
                        case (int)IOIOProtocolCommands.ESTABLISH_CONNECTION:
                            if (readByte() != 'I' || readByte() != 'O' || readByte() != 'I'
                                    || readByte() != 'O')
                            {
                                throw new IOException("Bad establish connection magic");
                            }
                            byte[] hardwareId = new byte[8];
                            byte[] bootloaderId = new byte[8];
                            byte[] firmwareId = new byte[8];
                            readBytes(8, hardwareId);
                            readBytes(8, bootloaderId);
                            readBytes(8, firmwareId);

                            Handler_.handleEstablishConnection(hardwareId, bootloaderId, firmwareId);
                            break;

                        case (int)IOIOProtocolCommands.SOFT_RESET:
                            AnalogFramePins_.Clear();
                            Handler_.handleSoftReset();
                            break;

                        case (int)IOIOProtocolCommands.REPORT_DIGITAL_IN_STATUS:
                            // Pin number and state are in same byte
                            arg1 = readByte();
                            Handler_.handleReportDigitalInStatus(arg1 >> 2, (arg1 & 0x01) == 1);
                            break;

                        case (int)IOIOProtocolCommands.SET_CHANGE_NOTIFY:
                            // Pin number and state are in same byte
                            arg1 = readByte();
                            Handler_.handleSetChangeNotify(arg1 >> 2, (arg1 & 0x01) == 1);
                            break;

                        case (int)IOIOProtocolCommands.REGISTER_PERIODIC_DIGITAL_SAMPLING:
                            // TODO: implement
                            break;

                        case (int)IOIOProtocolCommands.REPORT_PERIODIC_DIGITAL_IN_STATUS:
                            // TODO: implement
                            break;

                        case (int)IOIOProtocolCommands.REPORT_ANALOG_IN_FORMAT:
                            numPins = readByte();
                            NewFramePins_.Clear();
                            for (int i = 0; i < numPins; ++i)
                            {
                                NewFramePins_.Add(readByte());
                            }
                            calculateAnalogFrameDelta();
                            foreach (int i in RemovedPins_)
                            {
                                Handler_.handleAnalogPinStatus(i, false);
                            }
                            foreach (int i in AddedPins_)
                            {
                                Handler_.handleAnalogPinStatus(i, true);
                            }
                            break;

                        case (int)IOIOProtocolCommands.REPORT_ANALOG_IN_STATUS:
                            numPins = AnalogFramePins_.Count();
                            int header = 0;
                            AnalogPinValues_.Count();
                            for (int i = 0; i < numPins; ++i)
                            {
                                if (i % 4 == 0)
                                {
                                    header = readByte();
                                }
                                AnalogPinValues_.Add((readByte() << 2) | (header & 0x03));
                                header >>= 2;
                            }
                            Handler_.handleReportAnalogInStatus(AnalogFramePins_, AnalogPinValues_);
                            break;

                        case (int)IOIOProtocolCommands.UART_REPORT_TX_STATUS:
                            arg1 = readByte();
                            arg2 = readByte();
                            Handler_.handleUartReportTxStatus(arg1 & 0x03, (arg1 >> 2) | (arg2 << 6));
                            break;

                        case (int)IOIOProtocolCommands.UART_DATA:
                            arg1 = readByte();
                            size = (arg1 & 0x3F) + 1;
                            readBytes(size, data);
                            Handler_.handleUartData(arg1 >> 6, size, data);
                            break;

                        case (int)IOIOProtocolCommands.UART_STATUS:
                            arg1 = readByte();
                            if ((arg1 & 0x80) != 0)
                            {
                                Handler_.handleUartOpen(arg1 & 0x03);
                            }
                            else
                            {
                                Handler_.handleUartClose(arg1 & 0x03);
                            }
                            break;

                        case (int)IOIOProtocolCommands.SPI_DATA:
                            arg1 = readByte();
                            arg2 = readByte();
                            size = (arg1 & 0x3F) + 1;
                            readBytes(size, data);
                            Handler_.handleSpiData(arg1 >> 6, arg2 & 0x3F, data, size);
                            break;

                        case (int)IOIOProtocolCommands.SPI_REPORT_TX_STATUS:
                            arg1 = readByte();
                            arg2 = readByte();
                            Handler_.handleSpiReportTxStatus(arg1 & 0x03, (arg1 >> 2) | (arg2 << 6));
                            break;

                        case (int)IOIOProtocolCommands.SPI_STATUS:
                            arg1 = readByte();
                            if ((arg1 & 0x80) != 0)
                            {
                                Handler_.handleSpiOpen(arg1 & 0x03);
                            }
                            else
                            {
                                Handler_.handleSpiClose(arg1 & 0x03);
                            }
                            break;

                        case (int)IOIOProtocolCommands.I2C_STATUS:
                            arg1 = readByte();
                            if ((arg1 & 0x80) != 0)
                            {
                                Handler_.handleI2cOpen(arg1 & 0x03);
                            }
                            else
                            {
                                Handler_.handleI2cClose(arg1 & 0x03);
                            }
                            break;

                        case (int)IOIOProtocolCommands.I2C_RESULT:
                            arg1 = readByte();
                            arg2 = readByte();
                            if (arg2 != 0xFF)
                            {
                                readBytes(arg2, data);
                            }
                            Handler_.handleI2cResult(arg1 & 0x03, arg2, data);
                            break;

                        case (int)IOIOProtocolCommands.I2C_REPORT_TX_STATUS:
                            arg1 = readByte();
                            arg2 = readByte();
                            Handler_.handleI2cReportTxStatus(arg1 & 0x03, (arg1 >> 2) | (arg2 << 6));
                            break;

                        case (int)IOIOProtocolCommands.CHECK_INTERFACE_RESPONSE:
                            // this is 0x63 on my sparkfun 016 running 503 sw
                            arg1 = readByte();
                            Handler_.handleCheckInterfaceResponse((arg1 & 0x01) == 1);
                            break;

                        case (int)IOIOProtocolCommands.ICSP_REPORT_RX_STATUS:
                            arg1 = readByte();
                            arg2 = readByte();
                            Handler_.handleIcspReportRxStatus(arg1 | (arg2 << 8));
                            break;

                        case (int)IOIOProtocolCommands.ICSP_RESULT:
                            readBytes(2, data);
                            Handler_.handleIcspResult(2, data);
                            break;

                        case (int)IOIOProtocolCommands.ICSP_CONFIG:
                            arg1 = readByte();
                            if ((arg1 & 0x01) == 1)
                            {
                                Handler_.handleIcspOpen();
                            }
                            else
                            {
                                Handler_.handleIcspClose();
                            }
                            break;

                        case (int)IOIOProtocolCommands.INCAP_STATUS:
                            arg1 = readByte();
                            if ((arg1 & 0x80) != 0)
                            {
                                Handler_.handleIncapOpen(arg1 & 0x0F);
                            }
                            else
                            {
                                Handler_.handleIncapClose(arg1 & 0x0F);
                            }
                            break;

                        case (int)IOIOProtocolCommands.INCAP_REPORT:
                            arg1 = readByte();
                            size = arg1 >> 6;
                            if (size == 0)
                            {
                                size = 4;
                            }
                            readBytes(size, data);
                            Handler_.handleIncapReport(arg1 & 0x0F, size, data);
                            break;

                        case (int)IOIOProtocolCommands.SOFT_CLOSE:
                            LOG.Debug(IncomingTask_.Id + " Received soft close.");
                            throw new IOException("Soft close");

                        case (int)IOIOProtocolCommands.CAPSENSE_REPORT:
                            arg1 = readByte();
                            arg2 = readByte();
                            Handler_.handleCapSenseReport(arg1 & 0x3F, (arg1 >> 6) | (arg2 << 2));
                            break;

                        case (int)IOIOProtocolCommands.SET_CAPSENSE_SAMPLING:
                            arg1 = readByte();
                            Handler_.handleSetCapSenseSampling(arg1 & 0x3F, (arg1 & 0x80) != 0);
                            break;

                        case (int)IOIOProtocolCommands.SEQUENCER_EVENT:
                            arg1 = readByte();
                            // OPEN and STOPPED events has an additional argument.
                            if (arg1 == 2 || arg1 == 4)
                            {
                                arg2 = readByte();
                            }
                            else
                            {
                                arg2 = 0;
                            }
                            try
                            {
                                // should be able to cast since enums are really int (gag)
                                Handler_.handleSequencerEvent((SequencerEvent)arg1, arg2);
                            }
                            catch (Exception e)
                            {
                                throw new IOException("Unexpected eveent: " + arg1, e);
                            }
                            break;

                        case (int)IOIOProtocolCommands.SYNC:
                            Handler_.handleSync();
                            break;

                        default:
                            throw new ProtocolError("Received unexpected command: 0x"
                                    + arg1.ToString("X"));
                    }

                }
            }
            catch (System.Threading.ThreadAbortException e)
            {
                LOG.Error(IncomingTask_.Id + " Probably aborted thread (TAE): " + e.Message);
            }
            catch (ObjectDisposedException e)
            {
                //// see this when steram is closed
                LOG.Error(IncomingTask_.Id + " Probably closed incoming Stream: (ODE)" + e.Message);
            }
            catch (Exception e)
            {
                LOG.Error(IncomingTask_.Id + " Probably stopping incoming: (E)" + e.Message);
            }
            finally
            {
                // we don't play swith Stream since we didn't create it
                Handler_.handleConnectionLost();
                LOG.Info(IncomingTask_.Id + " Throwing thread cancel to stop incoming thread");
                CancelTokenSource_.Cancel();
                // debugger will always stop here in unit tests if test dynamically determines what port ot use
                // just hit continue in the debugger
                CancelTokenSource_.Token.ThrowIfCancellationRequested();
                Stream_ = null;
                this.IncomingTask_ = null;
            }
        }
    }
}
