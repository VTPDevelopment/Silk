﻿using System;
using SilkBot.Commands.Moderation.Utilities;

namespace SilkBot.Models
{
    public class UserInfractionModel
    {
        public int Id { get; set; } //Requisite Id for DB purposes
        public ulong UserId { get; set; }
        public ulong GuildId { get; set; }
        public string Reason { get; set; } // Why was this infraction given
        public ulong Enforcer { get; set; } //Who gave this infraction
        public UserModel User { get; set; } //Who's this affecting
        public DateTime InfractionTime { get; set; } //When it happened
        public InfractionType InfractionType { get; set; } //What happened
    }
}
