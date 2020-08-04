using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzBot.Epgp
{
    public class AliasService : IAliasService
    {
        private readonly BuzzBotDbContext _dbContext;

        public AliasService(BuzzBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public EpgpAlias GetActiveAlias(ulong userId)
        {
            var aliases = GetAliases(userId);
            var activeAlias = aliases.FirstOrDefault(a => a.IsActive);
            if (activeAlias == null)
            {
                activeAlias = GetPrimaryAlias(userId);
                activeAlias.IsActive = true;
                _dbContext.SaveChanges();
            }

            return activeAlias;
        }

        public EpgpAlias GetPrimaryAlias(ulong userId)
        {
            var aliases = GetAliases(userId);
            if (!aliases.Any(a => a.IsPrimary))
                throw new InvalidOperationException("No primary alias could be found for user.");
            return aliases.First(a => a.IsPrimary);
        }

        public void SetActiveAlias(ulong userId, string aliasName)
        {
            var currentActive = GetActiveAlias(userId);
            if (currentActive.Name.Equals(aliasName)) return;
            currentActive.IsActive = false;
            var aliases = GetAliases(userId);
            if (!aliases.Any(a => a.Name.Equals(aliasName)))
                throw new ArgumentException($"No alias named {aliasName} could be found for the specified user.");
            var newActive = aliases.First(a => a.Name.Equals(aliasName));
            newActive.IsActive = true;
            _dbContext.SaveChanges();
            ActiveAliasChanged?.Invoke(this, new AliasChangeEventArgs(userId, currentActive, newActive));
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
            PrimaryAliasChanged?.Invoke(this, new AliasChangeEventArgs(userId, currentPrimary, newPrimary));
        }

        public List<EpgpAlias> GetAliases(ulong userId)
        {
            return _dbContext.GuildUsers.Include(usr => usr.Aliases).ThenInclude(a => a.Transactions)
                .Include(usr => usr.Aliases).ThenInclude(a => a.AwardedItems).FirstOrDefault(usr => usr.Id == userId)
                ?.Aliases
                .ToList();
        }

        public EpgpAlias GetAlias(string aliasName)
        {
            return _dbContext.Aliases.FirstOrDefault(a => a.Name == aliasName);
        }

        public void AddAlias(EpgpAlias alias)
        {
            _dbContext.Aliases.Add(alias);
            _dbContext.SaveChanges();
            AliasAdded?.Invoke(this, alias);
        }

        public event EventHandler<AliasChangeEventArgs> PrimaryAliasChanged;
        public event EventHandler<AliasChangeEventArgs> ActiveAliasChanged;
        public event EventHandler<EpgpAlias> AliasAdded;
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
    }
}