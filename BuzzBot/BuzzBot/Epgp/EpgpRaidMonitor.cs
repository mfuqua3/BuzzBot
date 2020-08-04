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
using BuzzBotData.Data;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Epgp
{
    public class EpgpRaidMonitor : IDisposable
    {
        private readonly IEpgpService _epgpService;
        private CancellationTokenSource _cts;
        private readonly IEmoteService _emoteService;
        private RaidData _raidData;
        private readonly BuzzBotDbContext _dbContext;
        private bool _restart;
        private IAliasService _aliasService;

        public EpgpRaidMonitor(IEpgpService epgpService,  IEmoteService emoteService, RaidData raidData, BuzzBotDbContext dbContext, IAliasService aliasService)
        {
            _epgpService = epgpService;
            _emoteService = emoteService;
            _raidData = raidData;
            _dbContext = dbContext;
            _aliasService = aliasService;
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
            await messageChannel.SendMessageAsync("", false, award);
            var endTime = DateTime.UtcNow + _raidData.RaidObject.Duration;
            while (!_cts.Token.IsCancellationRequested && endTime > DateTime.UtcNow)
            {
                try
                {
                    await Task.Delay(_raidData.RaidObject.TimeBonusDuration, _cts.Token);
                    if (_raidData == null) return;
                    award = AwardEp(_raidData.RaidObject.TimeBonus, "Raid Time Bonus", GetAllUsers(_raidData.RaidObject));
                    await messageChannel.SendMessageAsync("", false, award);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            if (_raidData == null) return;
            award = AwardEp(_raidData.RaidObject.EndBonus, "Raid End Bonus", GetAllUsers(_raidData.RaidObject));
            await messageChannel.SendMessageAsync("", false, award);
            await messageChannel.SendMessageAsync("", false, BuildRaidSummary());
        }

        private Embed AwardEp(int value, string memo, List<RaidParticipant> participants)
        {
            var aliases = GetAliases(participants).Select(a => a.Name).Distinct().ToList();
            foreach (var alias in aliases)
            {
                _epgpService.Ep(alias, value, memo, TransactionType.EpAutomated);
            }
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle($"EP award of {value} - {memo}")
                .WithTimestamp(GetTimestamp());
            var count = participants.Count;
            const int numberOfColumns = 3;
            var columnMax = (int)Math.Ceiling((double)count / numberOfColumns);
            var remaining = participants;
            for (int i = 0; i < numberOfColumns; i++)
            {
                RaidParticipant[] forColumn;
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
                    var participantString = participant.IsPrimaryAlias ? $"<@{participant.Id}>" : participant.Alias;
                    columnSb.AppendLine($"{participantString}");
                }

                embedBuilder.AddField(EmbedConstants.EmptySpace, columnSb.ToString(), true);
            }

            return embedBuilder.Build();

        }

        private Embed BuildRaidSummary()
        {
            var raid = _dbContext.Raids
                .Include(r=>r.Loot).ThenInclude(ri=>ri.Item)
                .Include(r => r.Loot).ThenInclude(ri => ri.Transaction)
                .FirstOrDefault(r=>r.Id == _raidData.RaidObject.RaidId);
            //var raid = _epgpRepository.GetRaid(_raidData.RaidObject.RaidId);
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Raid Summary")
                .AddField("🌅 Start Time", raid.StartTime.ToEasternTime(), true)
                .AddField("🌙 End Time", raid.EndTime.ToEasternTime(), true)
                .AddField("💰 Loot distributed", raid.Loot.Count, true);

            var lootRecipients = raid.Loot.GroupBy(l => l.AwardedAliasId);
            foreach (var recipient in lootRecipients)
            {
                var alias = _aliasService.GetAlias(recipient.Key);
                var itemSb = new StringBuilder();
                foreach (var raidItem in recipient)
                {
                    itemSb.AppendLine($"{raidItem.Item.Name}");
                }

                embedBuilder.AddField(_emoteService.GetAliasString(alias, _raidData.ServerId), itemSb.ToString(), true);
            }

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

        private IEnumerable<EpgpAlias> GetAliases(IEnumerable<RaidParticipant> participants) =>
            participants.Select(p => _aliasService.GetAlias(p.Alias));

        public void Dispose()
        {
            _raidData.RaidObject.PropertyChanged -= RaidPropertyChanged;
            _cts?.Dispose();
            _raidData = null;
        }
    }
}