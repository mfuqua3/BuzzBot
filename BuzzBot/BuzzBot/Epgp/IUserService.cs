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
    }

    public class UserService : IUserService
    {
        private readonly IAliasService _aliasService;
        private EpgpRepository _epgpRepository;

        public UserService(IAliasService aliasService, EpgpRepository epgpRepository)
        {
            _aliasService = aliasService;
            _epgpRepository = epgpRepository;
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
                _epgpRepository.AddGuildUser(userId);
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