﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.ClassicGuildBank.Domain;
using BuzzBotData.Data;
using Discord.Net;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace BuzzBot.ClassicGuildBank.Buzz
{
    public class ClassicGuildBankClient
    {
        private readonly IConfiguration _configuration;
        private HttpClient _client;

        public ClassicGuildBankClient(IConfiguration configuration)
        {
            _configuration = configuration;
            _client = new HttpClient() { BaseAddress = new Uri("https://classicguildbankapi.azurewebsites.net/api/") };
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["bankApiToken"]);
        }

        public async Task<string> QueryCharacters(params string[] characters)
        {
            var characterObjects = await GetCharacters();
            var returnSb = new StringBuilder();
            foreach (var character in characterObjects)
            {
                if (!characters.Any(s => character.Name.Equals(s, StringComparison.InvariantCultureIgnoreCase))) continue;
                var countDictionary = new Dictionary<string, int>();
                returnSb.AppendLine($"Printing contents for {character.Name}:");
                returnSb.AppendLine($"{character.Gold / 10000:F0}G");
                foreach (var bagSlot in character.Bags.SelectMany(b => b.BagSlots))
                {
                    if (!countDictionary.ContainsKey(bagSlot.Item.Name))
                    {
                        countDictionary.Add(bagSlot.Item.Name, 0);
                    }

                    countDictionary[bagSlot.Item.Name] += bagSlot.Quantity;
                }

                foreach (var kvp in countDictionary)
                {
                    returnSb.AppendLine($"{kvp.Key} : {kvp.Value}");
                }
            }

            return returnSb.ToString();
        }

        public async Task<List<Guild>> GetGuilds()
        {
            var httpResult = await _client.GetAsync("guild/getguilds");
            if (httpResult.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("Unauthorized access, consider configuring an updated bank API token");
            }
            var guilds = JsonConvert.DeserializeObject<List<Guild>>(await httpResult.Content.ReadAsStringAsync());
            return guilds;
        }

        public async Task<List<Character>> GetCharacters(string guildId = null)
        {
            var guildToQuery = guildId ?? _configuration["buzzBankId"];
            var httpResult = await _client.GetAsync($"getcharacters/{guildToQuery}");

            if (httpResult.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException("Unauthorized access, consider configuring an updated bank API token");
            }
            var jsonResult = await httpResult.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Character>>(jsonResult);
        }
        public async Task<List<ItemQueryResult>> QueryItem(string itemName)
        {
            var result = new List<ItemQueryResult>();
            var characters = await GetCharacters();
            foreach (var character in characters)
            {
                foreach (var bag in character.Bags)
                {
                    foreach (var bagSlot in bag.BagSlots)
                    {
                        if (!bagSlot.Item.Name.Contains(itemName, StringComparison.InvariantCultureIgnoreCase)) continue;
                        if (!result.Any(r => r.CharacterName == character.Name && r.ItemName == bagSlot.Item.Name))
                        {
                            result.Add(new ItemQueryResult
                            {
                                ItemName = bagSlot.Item.Name,
                                CharacterName = character.Name
                            });
                        }

                        var thisResult = result.First((r =>
                            r.CharacterName == character.Name && r.ItemName == bagSlot.Item.Name));
                        thisResult.Quantity += bagSlot.Quantity;
                    }
                }
            }


            return result;
        }
    }
}