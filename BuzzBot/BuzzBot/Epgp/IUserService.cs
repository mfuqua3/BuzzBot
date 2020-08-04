using System;
using System.Threading.Tasks;
using BuzzBot.Discord.Extensions;
using BuzzBotData.Data;
using Discord;

namespace BuzzBot.Epgp
{
    public interface IUserService
    {
        Task<bool> TryAddUser(ulong userId, IGuild guild);
        bool UserExists(ulong userId);
        Task<bool> TryDeleteUser(ulong userId);
    }

    public class UserService : IUserService
    {
        private readonly IAliasService _aliasService;
        private readonly BuzzBotDbContext _dbContext;

        public UserService(IAliasService aliasService, BuzzBotDbContext dbContext)
        {
            _aliasService = aliasService;
            _dbContext = dbContext;
        }

        public bool UserExists(ulong userId)
        {
            return _dbContext.GuildUsers.Find(userId) != null;
        }

        public async Task<bool> TryDeleteUser(ulong userId)
        {
            var user = await _dbContext.GuildUsers.FindAsync(userId);
            if (user == null) return false;
            _dbContext.GuildUsers.Remove(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TryAddUser(ulong userId, IGuild guild)
        {
            if (guild == null) return false;
            var guildUser = await guild.GetUserAsync(userId);
            if (guildUser == null) return false;
            var userClass = guildUser.GetClass();
            if (userClass == WowClass.Unknown) return false;
            try
            {
                var alias = new EpgpAlias
                {
                    UserId = userId,
                    Class = userClass.ToDomainClass(),
                    EffortPoints = 0,
                    GearPoints = 0,
                    IsPrimary = true,
                    Name = guildUser.GetAliasName(),
                    Id = Guid.NewGuid()
                };
                _dbContext.GuildUsers.Add(new GuildUser { Id = userId });
                _aliasService.AddAlias(alias);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}