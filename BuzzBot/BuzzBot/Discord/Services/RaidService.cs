using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class RaidService : IRaidService
    {
        private readonly DiscordSocketClient _client;
        private readonly EpgpRepository _epgpRepository;
        private readonly IRaidMonitorFactory _raidMonitorFactory;
        private const int MaxConcurrentRaids = 1; //Only handles one raid in current version
        private readonly Dictionary<ulong, RaidData> _activeRaidMessages = new Dictionary<ulong, RaidData>();
        private readonly Dictionary<ulong, EpgpRaidMonitor> _raidMonitors = new Dictionary<ulong, EpgpRaidMonitor>();
        private readonly IEpgpConfigurationService _epgpConfigurationService;
        private readonly IEpgpService _epgpService;
        private readonly IAliasService _aliasService;
        private readonly IEmoteService _emoteService;

        public RaidService(
            DiscordSocketClient client,
            IRaidMonitorFactory raidMonitorFactory,
            EpgpRepository epgpRepository,
            IEpgpConfigurationService epgpConfigurationService,
            IEpgpService epgpService,
            IAliasService aliasService,
            IEmoteService emoteService)
        {
            _client = client;
            _epgpRepository = epgpRepository;
            _epgpConfigurationService = epgpConfigurationService;
            _epgpService = epgpService;
            _aliasService = aliasService;
            _emoteService = emoteService;
            _raidMonitorFactory = raidMonitorFactory;
            client.ReactionAdded += ReactionAdded;
            client.ReactionRemoved += ReactionRemoved;
            _aliasService.ActiveAliasChanged += ActiveAliasChanged;
        }

        private async void ActiveAliasChanged(object sender, AliasChangeEventArgs e)
        {
            foreach (var raid in _activeRaidMessages.Values.ToList())
            {
                if (!raid.RaidObject.Participants.ContainsKey(e.User)) continue;
                var participant = raid.RaidObject.Participants[e.User];
                participant.Alias = e.NewValue.Name;
                participant.WowClass = e.NewValue.Class.ToWowClass();
                participant.IsPrimaryAlias = e.NewValue.IsPrimary;

                var embed = CreateEmbed(raid.RaidObject, raid.ServerId);
                await raid.Message.ModifyAsync(opt => opt.Embed = embed);
            }
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
            if (!_epgpRepository.ContainsUser(reaction.UserId) &&
                !(await TryAddUser(reaction.UserId, (reaction.Channel as IGuildChannel)?.Guild))) return;
            var raid = _activeRaidMessages[reaction.MessageId];

            if (!(reaction.User.Value is IGuildUser)) return;
            var alias = _aliasService.GetActiveAlias(reaction.UserId);
            if (alias == null) return;
            var participant = new RaidParticipant(reaction.UserId, alias.Class.ToWowClass())
            {
                Alias = alias.Name,
                IsPrimaryAlias = alias.IsPrimary,
                Role = reaction.Emote.Name.ParseRoleFromEmote()
            };
            if (action == ReactionAction.Add)
            {
                raid.RaidObject.Participants.AddOrUpdate(participant.Id, id => participant,
                    (id, raidParticipant) => participant);
            }
            else if (action == ReactionAction.Remove)
            {
                if (raid.RaidObject.Participants.TryGetValue(reaction.UserId, out var existingParticipant) &&
                    existingParticipant.Role == participant.Role)
                {
                    raid.RaidObject.Participants.TryRemove(reaction.UserId, out _);
                }
            }
            var embed = CreateEmbed(raid.RaidObject, raid.ServerId);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }

        

        private async Task<bool> TryAddUser(ulong userId, IGuild guild)
        {
            if (guild == null) return false;
            var config = _epgpConfigurationService.GetConfiguration();
            var ep = config.EpMinimum;
            var gp = config.GpMinimum;
            var guildUser = await guild.GetUserAsync(userId);
            if (guildUser == null) return false;
            var userClass = guildUser.GetClass();
            if (userClass == WowClass.Unknown) return false;
            try
            {
                var alias = new EpgpAlias
                {
                    UserId = userId,
                    Class = userClass.ToDomainClass(),
                    EffortPoints = 0,
                    GearPoints = 0,
                    IsPrimary = true,
                    Name = guildUser.GetAliasName(),
                    Id = Guid.NewGuid()
                };
                _epgpRepository.AddGuildUser(userId);
                _epgpRepository.AddAlias(alias);
                _epgpService.Set(alias.Name, ep, gp, "User initialization");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private enum ReactionAction
        {
            Add,
            Remove
        }

        public async Task<ulong> PostRaid(IMessageChannel channel, EpgpRaid raidObject)
        {
            if (!(channel is IGuildChannel guildChannel))
            {
                throw new ArgumentException("Raids can only be posted to server text channels");
            }
            var guildId = guildChannel.GuildId; 
            var message = await channel.SendMessageAsync("", false, CreateEmbed(raidObject, guildChannel.GuildId), null);
            var raidData = new RaidData(message, raidObject, guildChannel.GuildId);
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.CasterEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.MeleeEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.RangedEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.TankEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.HealerEmoteName)));
            await message.AddReactionAsync(new Emoji("❌"));
            AddRaid(raidData);
            return raidData.Id;
        }

        public async Task KickUser(ulong userId, ulong raidId = 0)
        {
            if (raidId == 0) raidId = _activeRaidMessages.Keys.LastOrDefault();
            if (!_activeRaidMessages.ContainsKey(raidId))
                throw new InvalidOperationException("No active raid message was found for kick operation.");
            var raid = _activeRaidMessages[raidId];
            if (!raid.RaidObject.Participants.TryRemove(userId, out _))
            {
                throw new ArgumentException("No user by that name was found to kick");
            }
            var embed = CreateEmbed(raid.RaidObject, raid.ServerId);
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
            raidData.RaidObject.StartTime = DateTime.Now;
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

        private Embed CreateEmbed(EpgpRaid raidData, ulong guildId)
        {
            if (guildId == 0) throw new InvalidOperationException("Raid embed must be within a server channel");
            var casterEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.CasterEmoteName);
            var meleeEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.MeleeEmoteName);
            var rangedEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.RangedEmoteName);
            var tankEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.TankEmoteName);
            var healerEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.HealerEmoteName);
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

                .AddField($"{casterEmote} Casters ({GetParticipantCount(raidData.Participants.Values, Role.Caster)})", BuildUserList(raidData, Role.Caster, guildId), true)
                .AddField($"{meleeEmote} Melee ({GetParticipantCount(raidData.Participants.Values, Role.Melee)})", BuildUserList(raidData, Role.Melee, guildId), true)
                .AddField($"{rangedEmote} Ranged ({GetParticipantCount(raidData.Participants.Values, Role.Ranged)})", BuildUserList(raidData, Role.Ranged, guildId), true)
                .AddField($"{tankEmote} Tanks ({GetParticipantCount(raidData.Participants.Values, Role.Tank)})", BuildUserList(raidData, Role.Tank, guildId), true)
                .AddField($"{healerEmote} Healers ({GetParticipantCount(raidData.Participants.Values, Role.Healer)})", BuildUserList(raidData, Role.Healer, guildId), true)
                .AddField(EmbedConstants.EmptySpace, EmbedConstants.EmptySpace, true)
                .WithFooter((ftr) => ftr.WithText("\u200b\t\t\t\t\t\t\t\t\t\t\t\t\t\t\tStarts"))
                .WithTimestamp(raidData.StartTime);
            return embed.Build();
        }

        private int GetParticipantCount(ICollection<RaidParticipant> participants, Role role)
        {
            return participants.Count(p => p.Role == role);
        }

        private string BuildUserList(EpgpRaid raid, Role userRole, ulong guildId)
        {
            var participants = raid.Participants.Values;
            if (participants.All(p => p.Role != userRole))
            {
                return "None";
            }
            var returnSb = new StringBuilder();
            foreach (var participant in participants.Where(p => p.Role == userRole))
            {
                var fullyQualifiedEmoteName =
                    _emoteService.GetFullyQualifiedName(guildId, participant.WowClass.GetEmoteName());
                returnSb.AppendLine($"{fullyQualifiedEmoteName} {ParticipantString(participant)}");
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
        public ulong ServerId { get; }
        public IUserMessage Message { get; }
        public EpgpRaid RaidObject { get; }
        public ulong Id => Message.Id;
        public bool Started { get; set; }

        public RaidData(IUserMessage message, EpgpRaid raidObject, ulong serverId)
        {
            Message = message;
            RaidObject = raidObject;
            ServerId = serverId;
        }
    }
    public delegate Task<IUserMessage> ReplyDelegate(string message, bool isTts, Embed embed, RequestOptions options);
}