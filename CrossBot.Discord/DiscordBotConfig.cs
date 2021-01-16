﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossBot.Discord
{
    public class DiscordBotConfig
    {
        /// <summary> Custom Discord Status for playing a game. </summary>
        public string Name { get; set; } = "CrossBot";

        /// <summary> Bot login token. </summary>
        public string Token { get; set; } = "DISCORD_TOKEN";

        /// <summary> Bot command prefix. </summary>
        public string Prefix { get; set; } = "$";

        /// <summary> Users with this role are allowed to interact with the bot. If "@everyone", anyone can interact. </summary>
        public string RoleUseBot { get; set; } = "@everyone";

        // 64bit numbers white-listing certain channels/users for permission
        public List<ulong> Channels { get; set; } = new();
        public List<ulong> Users { get; set; } = new();
        public List<ulong> Sudo { get; set; } = new();
        public bool CanUseCommandUser(ulong authorId) => Users.Count == 0 || Users.Contains(authorId);
        public bool CanUseCommandChannel(ulong channelId) => Channels.Count == 0 || Channels.Contains(channelId);
        public bool CanUseSudo(ulong userId) => Sudo.Contains(userId);


        public bool GetHasRole(string roleName, IEnumerable<string> roles)
        {
            return roleName switch
            {
                nameof(RoleUseBot) => roles.Contains(RoleUseBot),
                _ => throw new ArgumentException($"{roleName} is not a valid role type.", nameof(roleName)),
            };
        }
    }
}