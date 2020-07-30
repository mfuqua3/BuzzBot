using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzBotData.Repositories
{
    public class GuildBankRepository
    {
        private readonly BuzzBotDbContext _dbContext;

        public GuildBankRepository(BuzzBotDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
            _dbContext = dbContext;
        }

        public void AddOrUpdateGuild(Guild guild)
        {
            var existingGuild = GetGuild(guild.Id);
            foreach (var character in existingGuild.Characters)
            {
                foreach (var bag in character.Bags)
                {
                    foreach (var bagSlot in bag.BagSlots)
                    {
                        _dbContext.BagSlots.Remove(bagSlot);
                    }

                    _dbContext.Bags.Remove(bag);
                }

                _dbContext.Remove(character);
            }

            _dbContext.Guilds.Remove(existingGuild);
            var characters = guild.Characters;
            guild.Characters = null;

            _dbContext.Guilds.Add(guild);
            Save();
            foreach (var character in characters)
            {
                var bags = character.Bags;
                character.Bags = null;
                character.Guild = null;
                var existingCharacter = _dbContext.Characters.FirstOrDefault(c => c.Id == character.Id);
                if (existingCharacter != null)
                {
                    _dbContext.Characters.Remove(existingCharacter);
                }
                _dbContext.Characters.Add(character);
                Save();
                foreach (var bag in bags)
                {
                    var bagslots = bag.BagSlots;

                    bag.BagSlots = null;
                    bag.Character = null;
                    bag.BagItem = null;
                    var existingBag = _dbContext.Bags.FirstOrDefault(bg => bg.Id == bag.Id);
                    if (existingBag != null)
                    {
                        _dbContext.Bags.Remove(existingBag);
                    }

                    _dbContext.Bags.Add(bag);
                    foreach (var bagSlot in bagslots)
                    {
                        bagSlot.ItemId = bagSlot.Item.Id;
                        bagSlot.Item = null;
                        bagSlot.Bag = null;
                        var existingBagSlot = _dbContext.BagSlots.FirstOrDefault(bs => bs.SlotId == bagSlot.SlotId && bs.BagId == bagSlot.BagId);
                        if (existingBagSlot != null)
                        {
                            _dbContext.BagSlots.Remove(existingBagSlot);
                        }

                        _dbContext.Add(bagSlot);
                    }
                }

            }
            Save();
        }

        public Guild GetGuild(Guid guildId)
        {
            return _dbContext.Guilds
                .Include(guild => guild.Characters)
                .ThenInclude(character => character.Bags)
                .ThenInclude(bag => bag.BagSlots)
                .ThenInclude(bagSlot => bagSlot.Item)
                .Include(guild => guild.Characters)
                .ThenInclude(character => character.Bags)
                .ThenInclude(bag => bag.BagItem)
                .FirstOrDefault(g => g.Id == guildId);
        }

        public IEnumerable<Character> GetCharacters()
        {
            return _dbContext.Guilds
                .Include(guild => guild.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagSlots)
                    .ThenInclude(bagSlot => bagSlot.Item)
                .Include(guild => guild.Characters)
                    .ThenInclude(character => character.Bags)
                    .ThenInclude(bag => bag.BagItem)
                .SingleOrDefault()
                .Characters;
        }

        public int GetTotalGold()
        {
            var characters = _dbContext.Characters.ToList();
            return _dbContext.Characters.Select(c => c.Gold).Sum();
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}