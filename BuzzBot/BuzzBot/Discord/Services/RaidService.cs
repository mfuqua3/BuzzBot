using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class RaidService : IRaidService, IDisposable
    {
        private const int MaxConcurrentRaids = 1; //Only handles one raid in current version
        private DiscordSocketClient _client;
        private readonly EpgpRepository _epgpRepository;
        private readonly IRaidMonitorFactory _raidMonitorFactory;
        private readonly IAliasService _aliasService;
        private readonly IEmoteService _emoteService;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IUserService _userService;
        private readonly IRaidRepository _raidRepository;

        public RaidService(
            DiscordSocketClient client,
            IRaidMonitorFactory raidMonitorFactory,
            EpgpRepository epgpRepository,
            IAliasService aliasService,
            IEmoteService emoteService,
            IEpgpCalculator epgpCalculator,
            IUserService userService, 
            IRaidRepository raidRepository)
        {
            _client = client;
            _epgpRepository = epgpRepository;
            _aliasService = aliasService;
            _emoteService = emoteService;
            _epgpCalculator = epgpCalculator;
            _userService = userService;
            _raidRepository = raidRepository;
            _raidMonitorFactory = raidMonitorFactory;
        }

        private async void ActiveAliasChanged(object sender, AliasChangeEventArgs e)
        {
            foreach (var raid in _raidRepository.GetRaids())
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
            if (!_raidRepository.Contains(reaction.MessageId)) return;
            if (!_epgpRepository.ContainsUser(reaction.UserId) &&
                !(await _userService.TryAddUser(reaction.UserId, (reaction.Channel as IGuildChannel)?.Guild))) return;
            var raid = _raidRepository.GetRaid(reaction.MessageId);

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

        private enum ReactionAction
        {
            Add,
            Remove
        }

        public async Task PostRaid(IMessageChannel channel, EpgpRaid raidObject)
        {
            if (!(channel is IGuildChannel guildChannel))
            {
                throw new ArgumentException("Raids can only be posted to server text channels");
            }
            var domainRaid = new Raid() { Id = new Guid(), StartTime = raidObject.StartTime, EndTime = raidObject.StartTime + raidObject.Duration, Name = raidObject.Name };
            _epgpRepository.PostRaid(domainRaid);
            raidObject.RaidId = domainRaid.Id;
            var guildId = guildChannel.GuildId;
            var message = await channel.SendMessageAsync("", false, CreateEmbed(raidObject, guildChannel.GuildId), null);
            var raidData = new RaidData(message, raidObject, guildChannel.GuildId);

            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _aliasService.ActiveAliasChanged += ActiveAliasChanged;
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.CasterEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.MeleeEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.RangedEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.TankEmoteName)));
            await message.AddReactionAsync(Emote.Parse(_emoteService.GetFullyQualifiedName(guildId, EmbedConstants.HealerEmoteName)));
            await message.AddReactionAsync(new Emoji("❌"));
            using (var raidMonitor = await AddRaid(raidData))
            {
                await raidMonitor.Run();
            }
            await RemoveRaid(raidData);
        }

        public EpgpRaid GetRaid(ulong raidId = 0)
        {
            var raidData = GetRaidData(raidId);
            return raidData.RaidObject;
        }

        public async Task KickUser(ulong userId, ulong raidId = 0)
        {
            var raid = _raidRepository.GetRaid(raidId);
            if (raid == null)
            {
                throw new InvalidOperationException("No active raid message was found to kick.");
            }
            if (!raid.RaidObject.Participants.TryRemove(userId, out _))
            {
                throw new ArgumentException("No user by that name was found to kick");
            }
            var embed = CreateEmbed(raid.RaidObject, raid.ServerId);
            await raid.Message.ModifyAsync(opt => opt.Embed = embed);
        }

        public async Task KickUser(string alias, ulong raidId = 0)
            => await KickUser(_epgpRepository.GetAlias(alias).UserId, raidId);

        public void Start(ulong raidId = 0)
        {
            var raidData = GetRaidData(raidId);
            if (raidData.Started)
                throw new InvalidOperationException("The raid has already started");
            raidData.RaidObject.StartTime = DateTime.UtcNow;
            var domainRaid = _epgpRepository.GetRaid(raidData.RaidObject.RaidId);
            domainRaid.StartTime = raidData.RaidObject.StartTime.ToUniversalTime();
            _epgpRepository.Save();

        }

        private RaidData GetRaidData(ulong raidId)
        {
            var raid = _raidRepository.GetRaid(raidId);
            if (raid == null)
                throw new InvalidOperationException("No active raid message was found to start.");
            return raid;
        }

        public void Extend(TimeSpan extend, ulong raidId = 0)
        {
            var raidData = GetRaidData(raidId);
            raidData.RaidObject.Duration += extend;
            var domainRaid = _epgpRepository.GetRaid(raidData.RaidObject.RaidId);
            domainRaid.EndTime += extend;
            _epgpRepository.Save();
        }

        public void End(ulong raidId = 0)
        {

            var raidData = GetRaidData(raidId);
            raidData.RaidObject.Duration = TimeSpan.Zero;
            var domainRaid = _epgpRepository.GetRaid(raidData.RaidObject.RaidId);
            domainRaid.EndTime = DateTime.UtcNow;
            _epgpRepository.Save();
        }

        private async Task<EpgpRaidMonitor> AddRaid(RaidData raidData)
        {
            var raidMonitor = _raidMonitorFactory.GetNew(raidData);
            raidData.RaidMonitor = raidMonitor;
            _raidRepository.AddOrUpdateRaid(raidData);
            while (_raidRepository.Count > MaxConcurrentRaids)
            {
                await RemoveRaid(_raidRepository.GetRaids().First());
            }

            return raidMonitor;
        }

        private async Task RemoveRaid(RaidData raidData)
        {
            _raidRepository.RemoveRaid(raidData.Id);
            if (raidData.RaidObject.Participants.Values.All(p => p.IsPrimaryAlias)) return;
            foreach (var participant in raidData.RaidObject.Participants.Values)
            {
                if (participant.IsPrimaryAlias) continue;
                _aliasService.SetActiveAlias(participant.Id, _aliasService.GetPrimaryAlias(participant.Id).Name);
            }

            await raidData.Message.Channel.SendMessageAsync("All users have been reset to their primary alias");
        }

        private Embed CreateEmbed(EpgpRaid raidData, ulong guildId)
        {
            if (guildId == 0) throw new InvalidOperationException("Raid embed must be within a server channel");
            var casterEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.CasterEmoteName);
            var meleeEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.MeleeEmoteName);
            var rangedEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.RangedEmoteName);
            var tankEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.TankEmoteName);
            var healerEmote = _emoteService.GetFullyQualifiedName(guildId, EmbedConstants.HealerEmoteName);
            var nexusCrystalString = raidData.NexusCrystalValue.ToGoldString();
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

                .AddField("__Notes__",
                    $"💎 Nexus Crystal Price: {nexusCrystalString}\n" +
                    $"🎲 Rolled Item GP Cost: {_epgpCalculator.ConvertGpFromGold(raidData.NexusCrystalValue) * 2} GP")
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

        public void Dispose()
        {
            _client.ReactionAdded -= ReactionAdded;
            _client.ReactionRemoved -= ReactionRemoved;
            _aliasService.ActiveAliasChanged -= ActiveAliasChanged;
            _client = null;
        }
    }
}