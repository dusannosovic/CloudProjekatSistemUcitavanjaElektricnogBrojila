﻿using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client
{
    public class ClientService : IClientService
    {
        public void Publish(string type)
        {
            ServiceEventSource.Current.Message("Klijent je obavesten");
        }
    }
}
