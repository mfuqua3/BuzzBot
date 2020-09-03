using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Utility;
using BuzzBot.Epgp;
using BuzzBot.Models;
using BuzzBotData.Data;
using Discord;
using Discord.WebSocket;

namespace BuzzBot.Discord.Services
{
    public class RaidService : IRaidService, IDisposable
    {
        private const int MaxConcurrentRaids = 1; //Only handles one raid in current version
        private DiscordSocketClient _client;
        private readonly IRaidMonitorFactory _raidMonitorFactory;
        private readonly IAliasEventAlerter _aliasEventAlerter;
        private readonly IEmoteService _emoteService;
        private readonly IEpgpCalculator _epgpCalculator;
        private readonly IUserService _userService;
        private readonly IAliasService _aliasService;
        private readonly IRaidRepository _raidRepository;
        private readonly IMapper _mapper;
        private readonly BuzzBotDbContext _dbContext;

        public RaidService(
            DiscordSocketClient client,
            IRaidMonitorFactory raidMonitorFactory,
            IAliasEventAlerter aliasEventAlerter,
            IEmoteService emoteService,
            IEpgpCalculator epgpCalculator,
            IUserService userService,
            IAliasService aliasService,
            IRaidRepository raidRepository,
            IMapper mapper,
            BuzzBotDbContext dbContext)
        {
            _client = client;
            _emoteService = emoteService;
            _epgpCalculator = epgpCalculator;
            _userService = userService;
            _aliasService = aliasService;
            _raidRepository = raidRepository;
            _mapper = mapper;
            _dbContext = dbContext;
            _raidMonitorFactory = raidMonitorFactory;
            _aliasEventAlerter = aliasEventAlerter;
        }

        private async void ActiveAliasChanged(object sender, AliasChangeEventArgs e)
        {
            foreach (var raid in _raidRepository.GetRaids())
            {
                if (!raid.RaidObject.Participants.ContainsKey(e.User)) continue;
                var aliasViewModels = _mapper.Map<ICollection<EpgpAlias>, List<EpgpAliasViewModel>>(e.NewValues);
                var participant = raid.RaidObject.Participants[e.User];
                participant.Aliases = aliasViewModels;
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
            if (!_userService.UserExists(reaction.UserId) &&
                !(await _userService.TryAddUser(reaction.UserId, (reaction.Channel as IGuildChannel)?.Guild))) return;
            var raid = _raidRepository.GetRaid(reaction.MessageId);

            if (!(reaction.User.Value is IGuildUser)) return;
            var aliases = _aliasService.GetActiveAliases(reaction.UserId).ToList();
            if (!aliases.Any()) return;
            var aliasViewModels = _mapper.Map<List<EpgpAlias>, List<EpgpAliasViewModel>>(aliases);
            var participant = new RaidParticipant(reaction.UserId)
            {
                Aliases = aliasViewModels,
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

            var leader = await guildChannel.GetUserAsync(raidObject.RaidLeader);
            IMessageChannel leaderChannel;
            if (leader == null)
            {
                leaderChannel = channel;
            }
            else
            {
                leaderChannel = await leader.GetOrCreateDMChannelAsync();
            }
            var domainRaid = new Raid() { Id = Guid.NewGuid(), StartTime = raidObject.StartTime, EndTime = raidObject.StartTime + raidObject.Duration, Name = raidObject.Name };
            _dbContext.Raids.Add(domainRaid);
            _dbContext.SaveChanges();
            raidObject.RaidId = domainRaid.Id;
            var guildId = guildChannel.GuildId;
            var message = await channel.SendMessageAsync("", false, CreateEmbed(raidObject, guildChannel.GuildId), null);
            var raidData = new RaidData(message, raidObject, guildChannel.GuildId);
            raidData.LeaderChannel = leaderChannel;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _aliasEventAlerter.ActiveAliasChanged += ActiveAliasChanged;
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
            => await KickUser(_dbContext.Aliases.FirstOrDefault(a=>a.Name==alias)?.UserId ?? 0, raidId);

        public void Start(ulong raidId = 0)
        {
            var raidData = GetRaidData(raidId);
            if (raidData.Started)
                throw new InvalidOperationException("The raid has already started");
            raidData.RaidObject.StartTime = DateTime.UtcNow;
            var raid = _dbContext.Raids.Find(raidData.RaidObject.RaidId);
            raid.StartTime = raidData.RaidObject.StartTime.ToUniversalTime();
            _dbContext.SaveChanges();
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
            var raid = _dbContext.Raids.Find(raidData.RaidObject.RaidId);
            raid.EndTime += extend;
            _dbContext.SaveChanges();
        }

        public void End(ulong raidId = 0)
        {

            var raidData = GetRaidData(raidId);
            var raid = _dbContext.Raids.Find(raidData.RaidObject.RaidId);
            raid.EndTime = DateTime.UtcNow;
            _dbContext.SaveChanges();
            raidData.RaidObject.Duration = TimeSpan.Zero;
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
            if (raidData.RaidObject.Participants.Values.All(p => p.Aliases.All(a => a.IsPrimary))) return;
            foreach (var participant in raidData.RaidObject.Participants.Values)
            {
                if (participant.Aliases.All(a=>a.IsPrimary)) continue;
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
            return participants.Where(p => p.Role == role).SelectMany(p=>p.Aliases).Count();
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
            foreach(var epgpAlias in participant.Aliases)
            {
                var fullyQualifiedEmoteName =
                    _emoteService.GetFullyQualifiedName(guildId, epgpAlias.Class.GetEmoteName());
                returnSb.AppendLine($"{fullyQualifiedEmoteName} {AliasString(epgpAlias)}");
            }

            return returnSb.ToString();
        }

        private string AliasString(EpgpAliasViewModel aliasViewModel)
        {
            return aliasViewModel.IsPrimary ? $"<@{aliasViewModel.UserId}>" : aliasViewModel.Name;
        }

        public void Dispose()
        {
            _client.ReactionAdded -= ReactionAdded;
            _client.ReactionRemoved -= ReactionRemoved;
            _aliasEventAlerter.ActiveAliasChanged -= ActiveAliasChanged;
            _client = null;
        }
    }
}