using System.Collections.Generic;
using System.Linq;
using Discord;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Discord.Services
{
    public class AdministrationService
    {
        private readonly IConfiguration _configuration;
        private readonly HashSet<ulong> _adminUsers;

        public AdministrationService(IConfiguration configuration)
        {
            _configuration = configuration;
            _adminUsers = configuration.GetSection("adminUsers").AsEnumerable()
                .Where(c => ulong.TryParse(c.Value, out _)).Select(c => ulong.Parse(c.Value)).ToHashSet();
        }
        public bool IsUserAdmin(IUser user)
        {
            return _adminUsers.Contains(user.Id);
        }

        public void Authorize(IUser user)
        {
           _adminUsers.Add(user.Id);
        }
    }
}