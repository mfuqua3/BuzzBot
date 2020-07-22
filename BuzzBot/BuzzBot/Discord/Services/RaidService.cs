﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Epgp;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class RaidService
    {
        public static string CasterEmote = $"<:{CasterEmoteName}:{CasterEmoteId}>";
        public static string MeleeEmote = $"<:{MeleeEmoteName}:{MeleeEmoteId}>";
        public static string RangedEmote = $"<:{RangedEmoteName}:{RangedEmoteId}>";
        public static string TankEmote = $"<:{TankEmoteName}:{TankEmoteId}>";
        public static string HealerEmote = $"<:{HealerEmoteName}:{HealerEmoteId}>";

        public const string WarriorEmote = "<:epgp_warrior:632577999664971776>";
        public const string PaladinEmote = "<:epgp_paladin:632578774063382548>";
        public const string HunterEmote = "<:epgp_hunter:632577999559983114>";
        public const string ShamanEmote = "<:epgp_shaman:632577999329296405>";
        public const string DruidEmote = "<:epgp_druid:632577999211855895>";
        public const string RogueEmote = "<:epgp_rogue:632577999673360384>";
        public const string PriestEmote = "<:epgp_priest:632577999580954634>";
        public const string WarlockEmote = "<:epgp_warlock:632577999652519966>";
        public const string MageEmote = "<:epgp_mage:632579085708689419>";

        private Dictionary<WowClass, string> _emoteDictionary = new Dictionary<WowClass, string>
        {
            {WowClass.Warrior, WarriorEmote},
            {WowClass.Paladin, PaladinEmote},
            {WowClass.Hunter, HunterEmote},
            {WowClass.Shaman, ShamanEmote},
            {WowClass.Druid, DruidEmote},
            {WowClass.Rogue, RogueEmote},
            {WowClass.Priest, PriestEmote},
            {WowClass.Warlock, WarlockEmote},
            {WowClass.Mage, MageEmote},
            {WowClass.Unknown, String.Empty},
        };

        public const string CasterEmoteName = @"epgp_caster";
        public const string MeleeEmoteName = @"epgp_melee";
        public const string RangedEmoteName = @"epgp_ranged";
        public const string TankEmoteName = @"epgp_tank";
        public const string HealerEmoteName = @"epgp_healer";
        public const ulong CasterEmoteId = 632575256464195596;
        public const ulong MeleeEmoteId = 632575285635579935;
        public const ulong RangedEmoteId = 632575300110385152;
        public const ulong TankEmoteId = 632575312818864128;
        public const ulong HealerEmoteId = 632575274067951626;

        public const string EmptySpace = "\u200b";

        private readonly Dictionary<ulong, RaidData> _activeRaidMessages = new Dictionary<ulong, RaidData>();

        public RaidService(DiscordSocketClient client)
        {
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {
            if (reaction.User.Value.IsBot) return;
            if (_activeRaidMessages.ContainsKey(reaction.MessageId)) return;
            var raid = _activeRaidMessages[reaction.MessageId];
            HashSet<RaidParticipant> roleCollection;
            switch (reaction.Emote.Name)
            {
                case MeleeEmoteName:
                    roleCollection = raid.RaidObject.Melee;
                    break;
                case RangedEmoteName:
                    roleCollection = raid.RaidObject.Ranged;
                    break;
                case CasterEmoteName:
                    roleCollection = raid.RaidObject.Casters;
                    break;
                case HealerEmoteName:
                    roleCollection = raid.RaidObject.Healers;
                    break;
                case TankEmoteName:
                    roleCollection = raid.RaidObject.Tanks;
                    break;
                default:
                    roleCollection = new HashSet<RaidParticipant>();
                    break;
            }

            if (!(reaction.User.Value is IGuildUser guildUser)) return;
            var wowClass = guildUser.GetClass();
            var participant = new RaidParticipant(reaction.UserId, wowClass);
            roleCollection.Remove(participant);
            var embed = CreateEmbed(raid.RaidObject);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        {

            if (reaction.User.Value.IsBot) return;
            if (_activeRaidMessages.ContainsKey(reaction.MessageId)) return;
            var raid = _activeRaidMessages[reaction.MessageId];
            HashSet<RaidParticipant> roleCollection;
            switch (reaction.Emote.Name)
            {
                case MeleeEmoteName:
                    roleCollection = raid.RaidObject.Melee;
                    break;
                case RangedEmoteName:
                    roleCollection = raid.RaidObject.Ranged;
                    break;
                case CasterEmoteName:
                    roleCollection = raid.RaidObject.Casters;
                    break;
                case HealerEmoteName:
                    roleCollection = raid.RaidObject.Healers;
                    break;
                case TankEmoteName:
                    roleCollection = raid.RaidObject.Tanks;
                    break;
                default:
                    roleCollection = new HashSet<RaidParticipant>();
                    break;
            }

            if (!(reaction.User.Value is IGuildUser guildUser)) return;
            var wowClass = guildUser.GetClass();
            var participant = new RaidParticipant(reaction.UserId, wowClass);
            raid.RaidObject.Melee.Remove(participant);
            raid.RaidObject.Casters.Remove(participant);
            raid.RaidObject.Ranged.Remove(participant);
            raid.RaidObject.Healers.Remove(participant);
            raid.RaidObject.Tanks.Remove(participant);
            roleCollection.Add(participant);
            var embed = CreateEmbed(raid.RaidObject);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }


        public async Task<ulong> PostRaid(ReplyDelegate replyDelegate, EpgpRaid raidObject)
        {
            var message = await replyDelegate("", false, CreateEmbed(raidObject), null);
            var raidData = new RaidData(message, raidObject);
            _activeRaidMessages.Add(raidData.Id, raidData);
            await message.AddReactionAsync(Emote.Parse(CasterEmote));
            await message.AddReactionAsync(Emote.Parse(MeleeEmote));
            await message.AddReactionAsync(Emote.Parse(RangedEmote));
            await message.AddReactionAsync(Emote.Parse(TankEmote));
            await message.AddReactionAsync(Emote.Parse(HealerEmote));
            await message.AddReactionAsync(new Emoji("❌"));
            return raidData.Id;
        }

        private Embed CreateEmbed(EpgpRaid raidData)
        {
            var embed = new EmbedBuilder();
            embed
                .WithTitle("__Raid Event__")
                .AddField(":busts_in_silhouette: Joined", $"{raidData.Joined}/{raidData.Capacity}", true)
                .AddField(":crown: Raid Leader", $"<@{raidData.RaidLeader}>", true)
                .AddField(":hourglass: Duration", $"{raidData.Duration.Hours} hrs {raidData.Duration.Minutes} mins", true)
                .AddField(":coffee: Start bonus", $"{raidData.StartBonus} EP", true)
                .AddField(":clock1: Time bonus", $"{raidData.TimeBonus} EP per {raidData.TimeBonusDuration.Minutes} mins", true)
                .AddField(":beers: End bonus", $"{raidData.EndBonus} EP", true)

                .AddField("__Roster__", EmptySpace)

                .AddField($"{CasterEmote} Casters ({raidData.Casters.Count})", BuildUserList(raidData.Casters), true)
                .AddField($"{MeleeEmote} Melee ({raidData.Melee.Count})", BuildUserList(raidData.Melee), true)
                .AddField($"{RangedEmote} Ranged ({raidData.Ranged.Count})", BuildUserList(raidData.Ranged), true)
                .AddField($"{TankEmote} Tanks ({raidData.Tanks.Count})", BuildUserList(raidData.Tanks), true)
                .AddField($"{HealerEmote} Healers ({raidData.Healers.Count})", BuildUserList(raidData.Healers), true)
                .AddField(EmptySpace, EmptySpace, true)
                .WithFooter((ftr) => ftr.WithText("\u200b\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tStarts"))
                .WithTimestamp(raidData.StartTime);
            return embed.Build();
        }
        private string BuildUserList(HashSet<RaidParticipant> userIdList)
        {
            if (!userIdList.Any())
            {
                return "None";
            }
            var returnSb = new StringBuilder();
            foreach (var participant in userIdList)
            {
                returnSb.AppendLine($"{_emoteDictionary[participant.WowClass]} <@{participant.Id}>");
            }

            return returnSb.ToString();
        }

        private class RaidData
        {
            public IUserMessage Message { get; }
            public EpgpRaid RaidObject { get; }
            public ulong Id => Message.Id;

            public RaidData(IUserMessage message, EpgpRaid raidObject)
            {
                Message = message;
                RaidObject = raidObject;
            }
        }
    }

    public delegate Task<IUserMessage> ReplyDelegate(string message, bool isTts, Embed embed, RequestOptions options);
}