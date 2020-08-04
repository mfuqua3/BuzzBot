using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuzzBot.NexusHub.Models;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.NexusHub
{
    public class NexusHubItemPoller
    {
        private readonly IConfiguration _configuration;
        private readonly NexusHubClient _nexusHubClient;
        private CancellationTokenSource _cts;
        private const string Server = "Kromcrush";
        private const string Faction = "Horde";

        public NexusHubItemPoller(IConfiguration configuration, NexusHubClient nexusHubClient)
        {
            _configuration = configuration;
            _nexusHubClient = nexusHubClient;
            _cts = new CancellationTokenSource();
        }

        public void Initialize()
        {
            Task.Factory.StartNew(
                PollItemDataAsync,
                _cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private async Task PollItemDataAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                var httpTimeout = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                OverviewResponseViewModel itemData;
                try
                {

                    itemData = await _nexusHubClient.GetItemOverview($"{Server}-{Faction}", httpTimeout.Token);
                }
                catch (TaskCanceledException)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    continue;
                }
                if (itemData != null)
                {
                    await using var context = new BuzzBotDbContext(_configuration);
                    var server = context.Servers.Include(s => s.Factions).ThenInclude(f => f.ItemData)
                        .FirstOrDefault(s => s.Id == Server);
                    if (server == null)
                    {
                        server = new Server() { Id = Server, Factions = new List<Faction>() };
                        context.Servers.Add(server);

                        await context.SaveChangesAsync(_cts.Token);
                    }

                    var faction = server.Factions.FirstOrDefault(f => f.Id == $"{Server}-{Faction}");
                    if (faction == null)
                    {
                        faction = new Faction { Id = $"{Server}-{Faction}", ServerId = server.Id, ItemData = new List<LiveItemData>() };
                        server.Factions.Add(faction);
                        context.Factions.Add(faction);
                        await context.SaveChangesAsync(_cts.Token);

                    }
                    foreach (var item in itemData.Data)
                    {
                        if (context.Items.Find(item.ItemId) == null)
                        {
                            continue;
                        }
                        var existingItem = faction.ItemData.FirstOrDefault(f => f.ItemId == item.ItemId);
                        if (existingItem == null)
                        {
                            existingItem = new LiveItemData { FactionId = faction.Id, ItemId = item.ItemId };
                            faction.ItemData.Add(existingItem);
                            context.LiveItemData.Add(existingItem);
                        }

                        existingItem.HistoricalValue = item.HistoricalValue ?? 0;
                        existingItem.MarketValue = item.MarketValue ?? 0;
                        existingItem.MinimumBuyout = item.MinimumBuyout ?? 0;
                        existingItem.NumberOfAuctions = item.NumberAuctions ?? 0;
                        existingItem.Quantity = item.Quantity ?? 0;


                    }
                    await context.SaveChangesAsync(_cts.Token);
                }

                await Task.Delay(TimeSpan.FromHours(1), _cts.Token);
            }
        }
    }
}