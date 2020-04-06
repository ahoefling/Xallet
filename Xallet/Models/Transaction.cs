﻿using System;

namespace Xallet.Models
{
    public class Transaction
    {
        public string Hash { get; set; }
        public string PublicAddress { get; set; }
        public double Tokens { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
