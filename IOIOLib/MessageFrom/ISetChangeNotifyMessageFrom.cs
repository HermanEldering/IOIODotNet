﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOIOLib.MessageFrom
{
    public interface ISetChangeNotifyMessageFrom : IMessageFromIOIO
    {
        int Pin { get; }
        bool ChangeNotify { get; }
    }
}
