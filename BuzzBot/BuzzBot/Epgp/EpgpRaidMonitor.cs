using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBot.Models;
using BuzzBotData.Data;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzBot.Epgp
{
    public class EpgpRaidMonitor : IDisposable
    {
        private CancellationTokenSource _cts;
        private readonly IEmoteService _emoteService;
        private RaidData _raidData;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private bool _restart;

        public EpgpRaidMonitor(IEmoteService emoteService, RaidData raidData, IServiceScopeFactory serviceScopeFactory)
        {
            _emoteService = emoteService;
            _raidData = raidData;
            _serviceScopeFactory = serviceScopeFactory;
            _cts = new CancellationTokenSource();
            _raidData.RaidObject.PropertyChanged += RaidPropertyChanged;
        }

        private void RaidPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(EpgpRaid.StartTime):
                    UpdateRaidStart();
                    return;
                case nameof(EpgpRaid.Duration):
                    if (!_raidData.Started)
                    {
                        UpdateRaidStart();
                        return;
                    }
                    UpdateRaidDuration();
                    return;
            }
        }

        private void UpdateRaidDuration()
        {
            if (_raidData.RaidObject.StartTime + _raidData.RaidObject.Duration < GetTimestamp()) return;
            _cts.Cancel();
        }

        private void UpdateRaidStart()
        {
            if (_raidData.Started) return;
            if (_raidData.RaidObject.Duration == TimeSpan.Zero)
            {
                _cts.Cancel();
                return;
            }
            _restart = true;
            _cts.Cancel();

        }

        public async Task Run()
        {
            var messageChannel = _raidData.Message.Channel;
            if (_raidData == null) return;
            if (_raidData.RaidObject.StartTime.ToUniversalTime() > DateTime.UtcNow && !_raidData.Started)
            {
                try
                {
                    var startDelay = _raidData.RaidObject.StartTime - GetTimestamp().ToUniversalTime();
                    if (startDelay > TimeSpan.Zero)
                        await Task.Delay(startDelay, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    if (!_restart) return;
                    _cts = new CancellationTokenSource();
                    _restart = false;
                    await Run();
                    return;
                }
            }

            _raidData.Started = true;
            var award = AwardEp(_raidData.RaidObject.StartBonus, "Raid Start Bonus", GetAllUsers(_raidData.RaidObject));
            await _raidData.LeaderChannel.SendMessageAsync("", false, award);
            if (_raidData.LeaderChannel != messageChannel)
            {
                await messageChannel.SendMessageAsync("", false, AlertEmbed("The Raid has begun"));
            }
            var endTime = DateTime.UtcNow + _raidData.RaidObject.Duration;
            while (!_cts.Token.IsCancellationRequested && endTime > DateTime.UtcNow)
            {
                try
                {
                    await Task.Delay(_raidData.RaidObject.TimeBonusDuration, _cts.Token);
                    if (_raidData == null) return;
                    award = AwardEp(_raidData.RaidObject.TimeBonus, "Raid Time Bonus", GetAllUsers(_raidData.RaidObject));
                    await _raidData.LeaderChannel.SendMessageAsync("", false, award);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            if (_raidData == null) return;
            award = AwardEp(_raidData.RaidObject.EndBonus, "Raid End Bonus", GetAllUsers(_raidData.RaidObject));
            await _raidData.LeaderChannel.SendMessageAsync("", false, award);
            await messageChannel.SendMessageAsync("", false, BuildRaidSummary());
        }


        private Embed AwardEp(int value, string memo, List<RaidParticipant> participants)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var epgp = scope.ServiceProvider.GetRequiredService<IEpgpService>();
            foreach (var participant in participants)
            {
                if (participant.Aliases.Count == 1)
                {
                    epgp.Ep(participant.Aliases[0].Name, value, memo, TransactionType.EpAutomated);
                    continue;
                }

                foreach (var alias in participant.Aliases)
                {
                    var ep = alias.IsPrimary ? value : (int)Math.Floor((double)value / 2); //Award full value if primary, otherwise add half (for multiboxers)
                    epgp.Ep(alias.Name, ep, memo, TransactionType.EpAutomated);
                }
            }
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle($"EP award of {value} - {memo}")
                .WithTimestamp(GetTimestamp());
            var count = participants.SelectMany(p=>p.Aliases).Count();
            const int numberOfColumns = 3;
            var columnMax = (int)Math.Ceiling((double)count / numberOfColumns);
            var remaining = participants.SelectMany(p=>p.Aliases).ToList();
            for (int i = 0; i < numberOfColumns; i++)
            {
                EpgpAliasViewModel[] forColumn;
                var columnSb = new StringBuilder();
                if (!remaining.Any())
                {
                    columnSb.AppendLine(EmbedConstants.EmptySpace);
                    embedBuilder.AddField(EmbedConstants.EmptySpace, columnSb.ToString());
                    continue;
                }
                if (i != numberOfColumns - 1)
                {
                    forColumn = remaining.Take(columnMax).ToArray();
                    remaining.RemoveRange(0, columnMax);
                }
                else
                {
                    forColumn = remaining.ToArray();
                }

                foreach (var participant in forColumn)
                {
                    var participantString = participant.IsPrimary ? $"<@{participant.UserId}>" : participant.Name;
                    columnSb.AppendLine($"{participantString}");
                }

                embedBuilder.AddField(EmbedConstants.EmptySpace, columnSb.ToString(), true);
            }

            return embedBuilder.Build();

        }

        private Embed BuildRaidSummary()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BuzzBotDbContext>();
            var raid = db.Raids
                .Include(r => r.Loot).ThenInclude(ri => ri.Item)
                .Include(r => r.Loot).ThenInclude(ri => ri.Transaction)
                .FirstOrDefault(r => r.Id == _raidData.RaidObject.RaidId);
            if (raid == null) return new EmbedBuilder().Build();
            var startTime = raid.StartTime;
            startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Raid Summary")
                .AddField("🌅 Start Time", startTime.ToEasternTime(), true)
                .AddField("🌙 End Time", DateTime.UtcNow.ToEasternTime(), true)
                .AddField("💰 Loot distributed", raid.Loot.Count, true);

            var lootRecipients = raid.Loot.GroupBy(l => l.AwardedAliasId);
            foreach (var recipient in lootRecipients)
            {
                var alias = db.Aliases.Find(recipient.Key);
                var itemSb = new StringBuilder();
                foreach (var raidItem in recipient)
                {
                    itemSb.AppendLine($"{raidItem.Item.Name}");
                }

                embedBuilder.AddField(_emoteService.GetAliasString(alias, _raidData.ServerId), itemSb.ToString(),
                    true);
            }

            return embedBuilder.Build();
        }

        private Embed AlertEmbed(string alertMessage)
        {
            var embedBuilder = new EmbedBuilder().WithCurrentTimestamp();
            embedBuilder.AddField("Alert", alertMessage);
            return embedBuilder.Build();
        }

        private DateTime GetTimestamp()
        {
            return DateTime.Now;
        }

        private List<RaidParticipant> GetAllUsers(EpgpRaid raid)
        {
            var returnList = new List<RaidParticipant>();
            returnList.AddRange(raid.Participants.Values);
            return returnList;
        }


        public void Dispose()
        {
            _raidData.RaidObject.PropertyChanged -= RaidPropertyChanged;
            _cts?.Dispose();
            _raidData = null;
        }
    }
}