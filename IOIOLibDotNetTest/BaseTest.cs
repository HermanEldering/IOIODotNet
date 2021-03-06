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

using IOIOLib.Connection;
using IOIOLib.Connection.Impl;
using IOIOLib.Device;
using IOIOLib.Device.Impl;
using IOIOLib.Message;
using IOIOLib.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace IOIOLibDotNetTest
{
    [TestClass]
    public class BaseTest
    {
        private static IOIOLog LOG = IOIOLogManager.GetLogger(typeof(BaseTest));

        public BaseTest()
        {
        }

        /// <summary>
        /// Capture all connections here so we can make sure we clean them up
        /// </summary>
        private List<IOIOConnection> ConnectionsOpenedDuringTest;
        /// <summary>
        /// Capture all devices here so we can make sure we clean them up
        /// </summary>
        private List<IOIO> DevicesOpenedDuringTest;
        /// <summary>
        /// Connection variable used by tests to talk to IOIO
        /// </summary>
        private IOIOConnection GoodConnection_ = null;

        internal ObserverLogAndCaptureLog CapturedLogs_;
        internal ObserverConnectionState CapturedConnectionState_;
        internal ObserverCaptureSingleQueue CapturedSingleQueueAllType_;
        /// <summary>
        /// This handler exists to support incoming/outgoing protocol tests with no IOIOImpl
        /// </summary>
        internal IObservableHandlerIOIO HandlerObservable_;

        /// <summary>
        /// Create new GoodConnection_ test collections before each test
        /// </summary>
        [TestInitialize()]
        public void MyTestInitialize()
        {
            ConnectionsOpenedDuringTest = new List<IOIOConnection>();
            DevicesOpenedDuringTest = new List<IOIO>();
            GoodConnection_ = null;
            LOG.Debug("Done MyTestInitialize");

        }


        /// <summary>
        /// Close all opened IOIO connections. Do this here instead of tests 
        /// so that it gets done even if tests fail or throw excpeiotns
        /// </summary>
        [TestCleanup()]
        public void MyTestCleanup()
        {
            DevicesOpenedDuringTest.ForEach(x =>
            {
                //x.Sync();
                // would like to reset so board is in original state every time we connect
                x.SoftReset();   // resets state without dropping connection
                // would we do this and wait instead
                //x.HardReset(); // like disconnecting the power
                x.Disconnect();
                LOG.Info("Disconnected " + x.ToString());
            });
            ConnectionsOpenedDuringTest.ForEach(x =>
                {
                    if (x.CanClose())
                    {
                        x.Disconnect();
                        LOG.Info("Disconnected " + x.ToString());
                    }
                });
            GoodConnection_ = null;

            CapturedLogs_ = null;
            CapturedConnectionState_ = null;
            CapturedSingleQueueAllType_ = null;
            HandlerObservable_ = null;

            System.Threading.Thread.Sleep(100);
            LOG.Debug("Done MyTestCleanup");
        }

        /// <summary>
        /// Creates a "good" serial GoodConnection_ and registeres it for automatic closure
        /// </summary>
        /// <param name="leaveConnectionOpen">defaults to true because that is the way the first tests ran.
        ///     set to false for IOIOImpl</param>
        /// <returns>connected that is set on instance variable</returns>
        internal IOIOConnection CreateGoodSerialConnection(bool leaveConnectionOpen = true)
        {
            IOIOConnectionFactory factory = new SerialConnectionFactory();
            GoodConnection_ = factory.CreateConnection(TestHarnessSetup.GOOD_CONN_NAME);
            this.ConnectionsOpenedDuringTest.Add(GoodConnection_); // always add connections used by incoming
            if (leaveConnectionOpen)
            {
                GoodConnection_.WaitForConnect(); // actually IsOpen the GoodConnection_
            }
            LOG.Debug("Done CreateGoodSerialConnection");
            return GoodConnection_;

        }

        /// <summary>
        /// Creates a standard HandlerContainer_ set and put it in instance variables so all tests can use.
        /// Create one of each of the standard types
        /// </summary>
        internal void CreateCaptureLogHandlerSet()
        {
            // create handlers of our own so we don't have to peek in and understand how IOIOImpl is configured

            CapturedSingleQueueAllType_ = new ObserverCaptureSingleQueue();
            CapturedLogs_ = new ObserverLogAndCaptureLog(10);
            CapturedConnectionState_ = new ObserverConnectionState();
            HandlerObservable_ = new IOIOHandlerObservableNoWait();
            HandlerObservable_.Subscribe(CapturedLogs_);
            HandlerObservable_.Subscribe(CapturedConnectionState_);
            HandlerObservable_.Subscribe(CapturedSingleQueueAllType_);

        }

        /// <summary>
        /// Use this to create your IOIO because it retains references to Tasks that are automatically cleaned up for you
        /// This should probably changed to take a list of observers instead
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        internal IOIO CreateIOIOImplAndConnect(IOIOConnection connection, List<IObserverIOIO> observers)
        {
            IOIO ourImpl = new IOIOImpl(connection, observers);
            DevicesOpenedDuringTest.Add(ourImpl);
            ourImpl.WaitForConnect();
            return ourImpl;
        }

    }
}
