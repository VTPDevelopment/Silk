﻿using System;

namespace SilkBot.ServerConfigurations.UserInfo
{
    [Serializable]
    public class BannedMember
    {
        public ulong ID { get; set; }
        public string Reason { get; set; }

        public BannedMember(ulong Id, string reason)
        {
            ID = Id;
            Reason = reason;
        }
    }
}