using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuzzBot.Discord.Services;
using BuzzBot.Epgp;
using BuzzBotData.Data;
using BuzzBotData.Repositories;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace BuzzBot.Discord.Modules
{

    [Group(GroupName)]
    public class EpgpModule : ModuleBase<SocketCommandContext>
    {
        private readonly EpgpRepository _repository;
        private readonly PriorityReportingService _priorityReportingService;
        private readonly QueryService _queryService;
        private readonly EpgpService _epgpService;
        private readonly AuditService _auditService;
        public const string GroupName = "epgp";

        public EpgpModule(EpgpRepository repository, PriorityReportingService priorityReportingService, QueryService queryService, EpgpService epgpService, AuditService auditService)
        {
            _repository = repository;
            _priorityReportingService = priorityReportingService;
            _queryService = queryService;
            _epgpService = epgpService;
            _auditService = auditService;
        }

        [Command("decay")]
        [RequiresBotAdmin]
        public async Task Decay(int percentage, string epgpFlag = null)
        {
            await _queryService.SendQuery($"Decay all user EP/GP values by {percentage}%?", Context.Channel,
                async () =>
                {
                    _epgpService.Decay(percentage, epgpFlag);
                    await ReplyAsync("Decay executed successfully.");
                },
                async () =>
                {
                    await ReplyAsync("Decay operation cancelled.");
                });
        }

        [Command("audit")]
        public async Task Audit(IGuildUser user) =>
            await Audit(string.IsNullOrEmpty(user.Nickname) ? user.Username : user.Nickname);

        [Command("audit")]
        public async Task Audit(string aliasName)
        {
            await _auditService.Audit(aliasName, Context.Channel);
        }
        

        [Command("pr")]
        public async Task PrintPriority()
        {
            await _priorityReportingService.ReportAll(Context.Channel);
        }

        [Command("pr")]
        public async Task PrintPriority(params string[] userNames)
        {
            await _priorityReportingService.ReportAliases(Context.Channel, userNames);
        }

        [Command("add")]
        [RequiresBotAdmin]
        public async Task AddUser(string username, int ep = 0, int gp = 0)
        {
            var user = Context.Guild.Users.FirstOrDefault(gu => gu.Username.StartsWith(username) || !string.IsNullOrWhiteSpace(gu.Nickname) && gu.Nickname.StartsWith(username));
            if (user == null)
            {
                await ReplyAsync("No user could be found with that name");
                return;
            }

            var id = user.Id;
            _repository.AddGuildUser(id);
            var userClass = GetClass(user);
            if (userClass == null)
            {
                await ReplyAsync("Unable to infer class for specified user. No primary alias will be created");
                return;
            }
            var alias = new EpgpAlias
            {
                UserId = id,
                Class = (Class)userClass,
                EffortPoints = ep,
                GearPoints = gp,
                IsPrimary = true,
                Name = username,
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            await ReplyAsync($"New user added with primary alias of \"{username} : {userClass}\"");
        }
        [Command("alias")]
        [RequiresBotAdmin]
        public async Task AddAlias(string userName, string aliasName, string className, int ep = 0, int gp = 0)
        {
            var user = Context.Guild.Users.FirstOrDefault(gu => gu.Username.StartsWith(userName) || !string.IsNullOrWhiteSpace(gu.Nickname) && gu.Nickname.StartsWith(userName));
            if (user == null)
            {
                await ReplyAsync($"No user could be found with that name: {userName}");
                return;
            }

            var id = user.Id;
            var userClass = GetClass(className);
            if (userClass == null)
            {
                await ReplyAsync("Unable to parse class from provided class argument");
                return;
            }
            var alias = new EpgpAlias
            {
                UserId = id,
                Class = (Class)userClass,
                EffortPoints = ep,
                GearPoints = gp,
                IsPrimary = false,
                Name = aliasName,
                Id = Guid.NewGuid()
            };
            _repository.AddAlias(alias);
            await ReplyAsync($"New alias add to {userName}: \"{aliasName} : {userClass}\"");
        }

        private Class? GetClass(IGuildUser guildUser)
        {
            var guild = guildUser.Guild;
            foreach (var roleId in guildUser.RoleIds)
            {
                var role = guild.GetRole(roleId);
                var userClass = GetClass(role.Name);
                if (userClass == null) continue;
                return userClass;
            }

            return null;
        }

        private Class? GetClass(string classString)
        {
            switch (classString)
            {
                case { } s when s.Equals("warrior", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Warrior;
                case { } s when s.Equals("paladin", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Paladin;
                case { } s when s.Equals("hunter", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Hunter;
                case { } s when s.Equals("shaman", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Shaman;
                case { } s when s.Equals("rogue", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Rogue;
                case { } s when s.Equals("druid", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Druid;
                case { } s when s.Equals("warlock", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Warlock;
                case { } s when s.Equals("priest", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Priest;
                case { } s when s.Equals("mage", StringComparison.CurrentCultureIgnoreCase):
                    return Class.Mage;
                default:
                    return null;
            }
        }
    }
}