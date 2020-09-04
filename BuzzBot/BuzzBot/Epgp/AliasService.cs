using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBot.Utility;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Epgp
{
    public interface IAliasConfiguration
    {
        /// <summary>
        /// Sets the alias to be the sole active alias (default behavior)
        /// </summary>
        void IsOnlyActive();
        /// <summary>
        /// Adds the alias an as additional active (multibox) alias.
        /// </summary>
        void AddAsMultibox();
    }
    public class AliasService : IAliasService
    {
        private readonly IAliasEventAlerter _eventAlerter;
        private readonly BuzzBotDbContext _dbContext;

        public AliasService(BuzzBotDbContext dbContext, IAliasEventAlerter eventAlerter)
        {
            _dbContext = dbContext;
            _eventAlerter = eventAlerter;
        }

      
        public IEnumerable<EpgpAlias> GetActiveAliases(ulong userId)
        {
            var aliases = GetAliases(userId);
            if (!aliases.Any()) return new EpgpAlias[] { };
            var activeAliases = aliases.Where(a => a.IsActive).ToArray();
            if (activeAliases.Any()) return activeAliases;

            var activeAlias = GetPrimaryAlias(userId);
            activeAlias.IsActive = true;
            _dbContext.SaveChanges();

            return new[] { activeAlias };
        }

        public EpgpAlias GetPrimaryAlias(ulong userId)
        {
            var aliases = GetAliases(userId);
            if (!aliases.Any(a => a.IsPrimary))
                throw new InvalidOperationException("No primary alias could be found for user.");
            return aliases.First(a => a.IsPrimary);
        }

        public void SetActiveAlias(ulong userId, string aliasName)
            => SetActiveAlias(userId, aliasName, opt => opt.IsOnlyActive());

        public void SetActiveAlias(ulong userId, string aliasName,
            Action<IAliasConfiguration> configurationOptions)
        {
            var config = new AliasConfiguration();
            configurationOptions(config);
            var currentActive = GetActiveAliases(userId).ToList();
            if (currentActive.Any(a => a.Name.Equals(aliasName) && config.IsMultiBoxAlias)) return;
            if (!config.IsMultiBoxAlias)
            {
                foreach (var alias in currentActive)
                {
                    alias.IsActive = false;
                }
            }
            var aliases = GetAliases(userId);
            if (!aliases.Any(a => a.Name.Equals(aliasName)))
                throw new ArgumentException($"No alias named {aliasName} could be found for the specified user.");
            var newActive = aliases.First(a => a.Name.Equals(aliasName));
            newActive.IsActive = true;
            _dbContext.SaveChanges();
            _eventAlerter.RaiseActiveAliasChanged(new AliasChangeEventArgs(userId, currentActive, aliases.Where(a => a.IsActive).ToList()));
        }

        public void SetPrimaryAlias(ulong userId, string aliasName)
        {
            var currentPrimary = GetPrimaryAlias(userId);
            if (currentPrimary.Name.Equals(aliasName)) return;
            currentPrimary.IsPrimary = false;
            var aliases = GetAliases(userId);
            if (!aliases.Any(a => a.Name.Equals(aliasName)))
                throw new ArgumentException($"No alias named {aliasName} could be found for the specified user.");
            var newPrimary = aliases.First(a => a.Name.Equals(aliasName));
            newPrimary.IsPrimary = true;
            _dbContext.SaveChanges();
            _eventAlerter.RaisePrimaryAliasChanged(new AliasChangeEventArgs(userId, new[] { currentPrimary }, new[] { newPrimary }));
        }

        public List<EpgpAlias> GetAliases(ulong userId)
        {
            return _dbContext.GuildUsers.Include(usr => usr.Aliases).ThenInclude(a => a.Transactions)
                .Include(usr => usr.Aliases).ThenInclude(a => a.AwardedItems).FirstOrDefault(usr => usr.Id == userId)
                ?.Aliases.ToList() ?? new List<EpgpAlias>();
        }

        public EpgpAlias GetAlias(string aliasName)
        {
            return _dbContext.Aliases.FirstOrDefault(a => a.Name == aliasName);
        }

        public void AddAlias(EpgpAlias alias)
        {
            _dbContext.Aliases.Add(alias);
            _dbContext.SaveChanges();
            _eventAlerter.RaiseAliasAdded(alias);
        }
        public EpgpAlias GetAlias(Guid aliasId)
        {
            return _dbContext.Aliases.Find(aliasId);
        }

        public void DeleteAlias(string aliasName)
        {
            var alias = GetAlias(aliasName);
            if (alias == null) return;
            _dbContext.Aliases.Remove(alias);
            _dbContext.SaveChanges();
        }

        protected class AliasConfiguration : IAliasConfiguration
        {
            public bool IsMultiBoxAlias { get; set; }
            public void IsOnlyActive()
            {
                IsMultiBoxAlias = false;
            }

            public void AddAsMultibox()
            {
                IsMultiBoxAlias = true;
            }
        }
    }
}