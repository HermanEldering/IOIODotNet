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
 
using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IOIOLib.Connection.Impl;
using IOIOLib.Connection;
using log4net.Config;
using IOIOLib.Util;
using System.IO;
using IOIOLib.IOIOException;

namespace IOIOLibDotNetTest.Connection.Impl
{
    /// <summary>
    /// Summary description for SerialConnectionFactoryTest
    /// </summary>
    [TestClass]
    public class SerialConnectionFactoryTest : BaseTest
    {
        private static IOIOLog LOG = IOIOLogManager.GetLogger(typeof(SerialConnectionFactoryTest));
        [TestMethod]
        public void SerialConnectionFactory_CreateConnections()
        {
            IOIOConnectionFactory factory = new SerialConnectionFactory();
            ICollection<IOIOConnection> connections = factory.CreateConnections();
            Assert.IsTrue(connections.Count > 0);
            LOG.Info("Found " + connections.Count + " possible com ports");

            /// probably don't need this since we aren't connected.
            foreach (IOIOConnection oneConn in connections)
            {
                oneConn.Disconnect();
            }
        }


        [TestMethod]
        [ExpectedExceptionAttribute(typeof(ConnectionCreationException))]
        public void SerialConnectionFactory_CreateConnectionBad()
        {
            IOIOConnectionFactory factory = new SerialConnectionFactory();
            IOIOConnection connection = factory.CreateConnection(TestHarnessSetup.BAD_CONN_NAME);
            LOG.Info("Should have failed test on " + TestHarnessSetup.BAD_CONN_NAME);
        }
    }
}
