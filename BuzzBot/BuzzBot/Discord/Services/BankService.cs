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
        private readonly IDbContextFactory _dbContextFactory;

        public BankService(IDbContextFactory dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public void AddOrUpdateGuild(Guild guild)
        {
            using var context = _dbContextFactory.GetNew();
            var existingGuild = context.Guilds
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
                            context.BagSlots.Remove(bagSlot);
                        }

                        context.Bags.Remove(bag);
                    }

                    context.Characters.Remove(character);
                }

                context.Guilds.Remove(existingGuild);
            }

            context.Guilds.Add(guild);
            context.SaveChanges();
        }

        public List<Character> GetBankCharacters()
        {
            using var context = _dbContextFactory.GetNew();
            return context.Characters
                .Include(c => c.Bags)
                .ThenInclude(b => b.BagSlots)
                .ThenInclude(bs => bs.Item)
                .ToList();
        }

        public int GetTotalGold()
        {
            using var context = _dbContextFactory.GetNew();
            return context.Characters.Sum(c => c.Gold);
        }
    }
}