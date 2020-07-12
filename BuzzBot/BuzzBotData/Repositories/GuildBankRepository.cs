using System;
using System.Linq;
using System.Threading.Tasks;
using BuzzBotData.Data;

namespace BuzzBotData.Repositories
{
    public class GuildBankRepository
    {
        private readonly GuildBankDbContext _dbContext;

        public GuildBankRepository(GuildBankDbContext dbContext)
        {
            dbContext.Database.EnsureCreated();
            _dbContext = dbContext;
        }

        public void AddOrUpdateGuild(Guild guild)
        {
            var existingGuild = _dbContext.Guilds.FirstOrDefault(g => g.Id == guild.Id);

            if (existingGuild != null)
                _dbContext.Guilds.Remove(existingGuild);
            _dbContext.Guilds.Add(guild);
            Save();
        }

        public void UpdateGuildCharacter(Character character, Guid guildId)
        {

            //var existingCharacter = _dbContext.Characters.FirstOrDefault(c => c.GuildId == guildId && c.Name == character.Name);

            //if (existingCharacter != null)
            //    _dbContext.Characters.Remove(existingCharacter);

            _dbContext.Characters.Add(character);

            Save();
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}