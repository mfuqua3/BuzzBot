using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Discord.Services
{
    public interface IBankService
    {
        List<Character> GetBankCharacters();
        int GetTotalGold();
        void AddOrUpdateGuild(Guild guild);
    }
    public class BankService:IBankService
    {
        private readonly BuzzBotDbContext _dbContext;

        public BankService(BuzzBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void AddOrUpdateGuild(Guild guild)
        {
            var existingGuild = _dbContext.Guilds
                .Include(g => g.Characters)
                .ThenInclude(c => c.Bags)
                .ThenInclude(b => b.BagSlots)
                .FirstOrDefault(g=>g.Id == guild.Id);
            if(existingGuild!=null)
            {
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

                    _dbContext.Characters.Remove(character);
                }

                _dbContext.Guilds.Remove(existingGuild);
            }

            _dbContext.Guilds.Add(guild);
            _dbContext.SaveChanges();
        }

        public List<Character> GetBankCharacters()
        {
            return _dbContext.Characters
                .Include(c => c.Bags)
                .ThenInclude(b => b.BagSlots)
                .ThenInclude(bs => bs.Item)
                .ToList();
        }

        public int GetTotalGold()
        {
            return _dbContext.Characters.Sum(c => c.Gold);
        }
    }
}