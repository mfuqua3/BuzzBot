using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.ClassicGuildBank.Domain;
using BuzzBotData.Data;
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
                returnSb.AppendLine($"{character.Gold/10000:F0}G");
                foreach (var bagSlot in character.Bags.SelectMany(b=>b.BagSlots))
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
            var guilds = JsonConvert.DeserializeObject<List<Guild>>(await httpResult.Content.ReadAsStringAsync());
            return guilds;
        }

        public async Task<List<Character>> GetCharacters(string guildId = null)
        {
            var guildToQuery = guildId ?? _configuration["buzzBankId"];
            var httpResult = await _client.GetAsync($"getcharacters/{guildToQuery}");
            var jsonResult = await httpResult.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Character>>(jsonResult);
        }
        public async Task<List<ItemQueryResult>> QueryItem(string itemName)
        {
            var result = new List<ItemQueryResult>();
            var characters = await GetCharacters();
            foreach (var character in characters)
            {
                var count = 0;
                foreach (var bag in character.Bags)
                {
                    foreach (var bagSlot in bag.BagSlots)
                    {
                        if (!bagSlot.Item.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase)) continue;
                        count += bagSlot.Quantity;
                    }
                }

                if (count > 0)
                {
                    result.Add(new ItemQueryResult
                    {
                        ItemName = itemName,
                        CharacterName = character.Name,
                        Quantity = count
                    });
                }
            }


            return result;
        }
    }
}