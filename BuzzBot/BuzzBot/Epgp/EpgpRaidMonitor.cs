using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Discord.Utility;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;

namespace BuzzBot.Epgp
{
    public class EpgpRaidMonitor : IDisposable
    {
        private readonly IEpgpService _epgpService;
        private readonly EpgpRepository _epgpRepository;
        private Action _onRaidEndedAction;
        private EpgpRaid _raid;
        private CancellationTokenSource _cts;

        public EpgpRaidMonitor(IEpgpService epgpService, EpgpRepository epgpRepository, Action onRaidEndedAction)
        {
            _epgpService = epgpService;
            _epgpRepository = epgpRepository;
            _onRaidEndedAction = onRaidEndedAction;
        }

        public void UpdateRaid(RaidData data)
        {
            var raidExists = _raid != null;
            if (!raidExists)
            {
                AddRaid(data);
                return;
            }

            if (!_raid.Started)
            {
                _cts.Cancel();
                _raid = null;
                AddRaid(data);
                return;
            }

            _raid = data.RaidObject;
            if (_raid.StartTime.ToUniversalTime() + _raid.Duration > DateTime.UtcNow)
            {
                _cts.Cancel();
            }
        }

        public void RemoveRaid(RaidData data)
        {
            var raidExists = _raid != null;
            if (!raidExists) return;
            _cts.Cancel();
            if (_raid.Started)
            {
                return;
            }

            Dispose();
        }

        public void AddRaid(RaidData data)
        {
            var raidExists = _raid != null;
            if (raidExists)
            {
                UpdateRaid(data);
                return;
            }
            _cts = new CancellationTokenSource();
            _raid = data.RaidObject;

            Task.Factory.StartNew(() => RunRaidMonitor(
                    data.Message.Channel),
                TaskCreationOptions.LongRunning);
        }

        private async Task RunRaidMonitor(IMessageChannel messageChannel)
        {
            if (_raid == null) return;
            if (_raid.StartTime.ToUniversalTime() > DateTime.UtcNow && !_raid.Started)
            {
                try
                {
                    var startDelay = _raid.StartTime - GetTimestamp();
                    if (startDelay > TimeSpan.Zero)
                        await Task.Delay(startDelay, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            _raid.Started = true;
            await messageChannel.SendMessageAsync("", false, GetRaidAlertEmbed($"The raid has begun"));
            var award = AwardEp(_raid.StartBonus, "RAID START BONUS", GetAllUsers(_raid));
            await messageChannel.SendMessageAsync("", false, award);
            var endTime = DateTime.UtcNow + _raid.Duration;
            while (!_cts.Token.IsCancellationRequested && endTime > DateTime.UtcNow)
            {
                try
                {
                    await Task.Delay(_raid.TimeBonusDuration, _cts.Token);
                    if (_raid == null) return;
                    award = AwardEp(_raid.TimeBonus, "RAID TIME BONUS", GetAllUsers(_raid));
                    await messageChannel.SendMessageAsync("", false, award);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            if (_raid == null) return;
            await messageChannel.SendMessageAsync("", false, GetRaidAlertEmbed($"The raid has ended"));
            award = AwardEp(_raid.EndBonus, "RAID END BONUS", GetAllUsers(_raid));
            await messageChannel.SendMessageAsync("", false, award);
            Dispose();
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

        private DateTime GetTimestamp()
        {
            var tzi = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            var timeEst = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
            return timeEst;
        }

        private List<RaidParticipant> GetAllUsers(EpgpRaid raid)
        {
            var returnList = new List<RaidParticipant>();
            returnList.AddRange(raid.Melee);
            returnList.AddRange(raid.Casters);
            returnList.AddRange(raid.Ranged);
            returnList.AddRange(raid.Tanks);
            returnList.AddRange(raid.Healers);
            return returnList;
        }

        private List<string> GetAliases(EpgpRaid raid)
        {
            var aliases = new List<EpgpAlias>();
            aliases.AddRange(GetAliases(raid.Melee));
            aliases.AddRange(GetAliases(raid.Casters));
            aliases.AddRange(GetAliases(raid.Melee));
            aliases.AddRange(GetAliases(raid.Melee));
            aliases.AddRange(GetAliases(raid.Melee));
            return aliases.Select(a => a.Name).Distinct().ToList();
        }

        private IEnumerable<EpgpAlias> GetAliases(IEnumerable<RaidParticipant> participants) =>
            participants.Select(p => _epgpRepository.GetAlias(p.Alias));

        private Embed GetRaidAlertEmbed(string alertText)
        {
            var embedBuilder = new EmbedBuilder()
                .AddField("__Raid Alert__", alertText)
                .WithTimestamp(GetTimestamp());
            return embedBuilder.Build();
        }

        public void Dispose()
        {
            _onRaidEndedAction();
            _onRaidEndedAction = null;
            _cts?.Dispose();
            _raid = null;
        }
    }
}