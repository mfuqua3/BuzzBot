using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Repositories;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class RaidService : IRaidService
    {
        private readonly EpgpRepository _epgpRepository;
        private readonly IRaidMonitorFactory _raidMonitorFactory;
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

        private readonly Dictionary<WowClass, string> _emoteDictionary = new Dictionary<WowClass, string>
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

        private const int MaxConcurrentRaids = 1; //Only handles one raid in current version
        private readonly Dictionary<ulong, RaidData> _activeRaidMessages = new Dictionary<ulong, RaidData>();
        private readonly Dictionary<ulong, EpgpRaidMonitor> _raidMonitors = new Dictionary<ulong, EpgpRaidMonitor>();

        public RaidService(DiscordSocketClient client, IRaidMonitorFactory raidMonitorFactory, EpgpRepository epgpRepository)
        {
            _epgpRepository = epgpRepository;
            _raidMonitorFactory = raidMonitorFactory;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __, SocketReaction reaction)
        => await ReactionManipulated(reaction, ReactionAction.Remove);

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel __,
            SocketReaction reaction)
            => await ReactionManipulated(reaction, ReactionAction.Add);

        private async Task ReactionManipulated(SocketReaction reaction, ReactionAction action)
        {
            if (reaction.User.Value.IsBot) return;
            if (!_activeRaidMessages.ContainsKey(reaction.MessageId)) return;
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

            if (!(reaction.User.Value is IGuildUser)) return;
            var aliases = _epgpRepository.GetAliasesForUser(reaction.UserId);
            if (!aliases.Any()) return;
            var alias = aliases.FirstOrDefault(a => a.IsActive) ?? aliases.FirstOrDefault(a => a.IsPrimary) ?? aliases.First();
            var participant = new RaidParticipant(reaction.UserId, alias.Class.ToWowClass()) { Alias = alias.Name, IsPrimaryAlias = alias.IsPrimary };
            if (action == ReactionAction.Add)
            {

                RemoveUser(raid.RaidObject.Melee, reaction.UserId);
                RemoveUser(raid.RaidObject.Casters, reaction.UserId);
                RemoveUser(raid.RaidObject.Ranged, reaction.UserId);
                RemoveUser(raid.RaidObject.Healers, reaction.UserId);
                RemoveUser(raid.RaidObject.Tanks, reaction.UserId);
                roleCollection.Add(participant);
            }
            else if (action == ReactionAction.Remove)
            {
                RemoveUser(roleCollection, reaction.UserId);
            }
            var embed = CreateEmbed(raid.RaidObject);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }

        private enum ReactionAction
        {
            Add,
            Remove
        }

        public async Task<ulong> PostRaid(ReplyDelegate replyDelegate, EpgpRaid raidObject)
        {
            var message = await replyDelegate("", false, CreateEmbed(raidObject), null);
            var raidData = new RaidData(message, raidObject);
            await message.AddReactionAsync(Emote.Parse(CasterEmote));
            await message.AddReactionAsync(Emote.Parse(MeleeEmote));
            await message.AddReactionAsync(Emote.Parse(RangedEmote));
            await message.AddReactionAsync(Emote.Parse(TankEmote));
            await message.AddReactionAsync(Emote.Parse(HealerEmote));
            await message.AddReactionAsync(new Emoji("❌"));
            AddRaid(raidData);
            return raidData.Id;
        }

        public async Task KickUser(ulong userId, ulong raidId = 0)
        {
            if (raidId == 0) raidId = _activeRaidMessages.Keys.LastOrDefault();
            if(!_activeRaidMessages.ContainsKey(raidId))
                throw new InvalidOperationException("No active raid message was found for kick operation.");
            var raid = _activeRaidMessages[raidId];
            RemoveUser(raid.RaidObject.Melee, userId);
            RemoveUser(raid.RaidObject.Casters, userId);
            RemoveUser(raid.RaidObject.Ranged, userId);
            RemoveUser(raid.RaidObject.Healers, userId);
            RemoveUser(raid.RaidObject.Tanks, userId);
            var embed = CreateEmbed(raid.RaidObject);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }

        private void RemoveUser(ICollection<RaidParticipant> participantCollection, ulong user)
        {
            var existingParticipant = participantCollection.FirstOrDefault(rp => rp.Id == user);
            if (existingParticipant == null) return;
            participantCollection.Remove(existingParticipant);
        }

        public async Task KickUser(string alias, ulong raidId = 0)
            => await KickUser(_epgpRepository.GetAlias(alias).UserId, raidId);

        public void Start(ulong raidId = 0)
        {
            if (raidId == 0) raidId = _activeRaidMessages.Keys.LastOrDefault();
            if (!_activeRaidMessages.ContainsKey(raidId) || !_raidMonitors.ContainsKey(raidId))
                throw new InvalidOperationException("No active raid message was found to start.");
            var raidData = _activeRaidMessages[raidId];
            if (raidData.Started)
                throw new InvalidOperationException("The raid has already started");
            var raidMonitor = _raidMonitors[raidId];
            raidData.RaidObject.StartTime = DateTime.Now.ToEasternTime();
            raidMonitor.UpdateRaid(raidData);
        }

        public void Extend(TimeSpan extend, ulong raidId = 0)
        {
            if (raidId == 0) raidId = _activeRaidMessages.Keys.LastOrDefault();
            if (!_activeRaidMessages.ContainsKey(raidId) || !_raidMonitors.ContainsKey(raidId))
                throw new InvalidOperationException("No active raid message was found to extend.");
            var raidData = _activeRaidMessages[raidId];
            raidData.RaidObject.Duration += extend;
            _raidMonitors[raidId].UpdateRaid(raidData);
        }

        public void End(ulong raidId = 0)
        {
            if (raidId == 0) raidId = _activeRaidMessages.Keys.LastOrDefault();
            if (!_activeRaidMessages.ContainsKey(raidId) || !_raidMonitors.ContainsKey(raidId))
                throw new InvalidOperationException("No active raid message was found to extend.");
            var raidData = _activeRaidMessages[raidId];
            var raidMonitor = _raidMonitors[raidId];
            raidMonitor.RemoveRaid(raidData);
        }

        private void AddRaid(RaidData raidData)
        {
            _activeRaidMessages.Add(raidData.Id, raidData);
            var raidMonitor = _raidMonitorFactory.GetNew(() => RemoveRaid(raidData));
            raidMonitor.AddRaid(raidData);
            _raidMonitors.Add(raidData.Id, raidMonitor);
            while (_activeRaidMessages.Count > MaxConcurrentRaids)
            {
                RemoveRaid(_activeRaidMessages.First().Value);
            }
        }

        private void RemoveRaid(RaidData raidData)
        {
            _activeRaidMessages.Remove(raidData.Id);
            if (!_raidMonitors.TryGetValue(raidData.Id, out var monitor)) return;
            _raidMonitors.Remove(raidData.Id);
            monitor.RemoveRaid(raidData);
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

                .AddField("__Roster__", EmbedConstants.EmptySpace)

                .AddField($"{CasterEmote} Casters ({raidData.Casters.Count})", BuildUserList(raidData.Casters), true)
                .AddField($"{MeleeEmote} Melee ({raidData.Melee.Count})", BuildUserList(raidData.Melee), true)
                .AddField($"{RangedEmote} Ranged ({raidData.Ranged.Count})", BuildUserList(raidData.Ranged), true)
                .AddField($"{TankEmote} Tanks ({raidData.Tanks.Count})", BuildUserList(raidData.Tanks), true)
                .AddField($"{HealerEmote} Healers ({raidData.Healers.Count})", BuildUserList(raidData.Healers), true)
                .AddField(EmbedConstants.EmptySpace, EmbedConstants.EmptySpace, true)
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
                returnSb.AppendLine($"{_emoteDictionary[participant.WowClass]} {ParticipantString(participant)}");
            }

            return returnSb.ToString();
        }

        private string ParticipantString(RaidParticipant participant)
        {
            return participant.IsPrimaryAlias ? $"<@{participant.Id}>" : participant.Alias;
        }
    }
    public class RaidData
    {
        public IUserMessage Message { get; }
        public EpgpRaid RaidObject { get; }
        public ulong Id => Message.Id;
        public bool Started { get; set; }

        public RaidData(IUserMessage message, EpgpRaid raidObject)
        {
            Message = message;
            RaidObject = raidObject;
        }
    }
    public delegate Task<IUserMessage> ReplyDelegate(string message, bool isTts, Embed embed, RequestOptions options);
}