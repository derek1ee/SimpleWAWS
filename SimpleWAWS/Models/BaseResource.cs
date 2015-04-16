﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SimpleWAWS.Models
{
    public abstract class BaseResource
    {
        public string SubscriptionId { get; set; }

        public string ResourceGroupName { get; set; }

        public abstract string CsmId { get; }
    }
}