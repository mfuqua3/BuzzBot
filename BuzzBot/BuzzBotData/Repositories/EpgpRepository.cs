using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzBotData.Repositories
{
    public class EpgpRepository
    {
        private readonly BuzzBotDbContext _dbContext;

        public EpgpRepository(BuzzBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void AddGuildUser(ulong id)
        {
            var user = new GuildUser() { Id = id };
            _dbContext.GuildUsers.Add(user);
            _dbContext.SaveChanges();
        }

        public bool ContainsUser(ulong id)
        {
            return _dbContext.GuildUsers.Any(gu => gu.Id == id);
        }

        public void AddAlias(EpgpAlias alias)
        {
            if (alias.UserId == 0) throw new ArgumentException("Must provide a user id for the provided alias");
            if (!ContainsUser(alias.UserId))
                AddGuildUser(alias.UserId);
            _dbContext.Aliases.Add(alias);
            _dbContext.SaveChanges();
        }

        public ICollection<EpgpAlias> GetAliasesForUser(ulong user)
        {
            return _dbContext.Aliases.Where(a => a.UserId == user).ToList();
        }

        public ICollection<EpgpAlias> GetAliases()
        {
            return _dbContext.Aliases.ToList();
        }

        public EpgpAlias GetAlias(string name)
        {
            return _dbContext.Aliases.FirstOrDefault(a => a.Name.ToUpper().Contains(name.ToUpper()));
        }

        public EpgpAlias GetAlias(Guid id)
        {
            return _dbContext.Aliases.FirstOrDefault(a => a.Id == id);
        }

        public EpgpAlias GetPrimaryAlias(ulong userId)
        {
            return GetAliasesForUser(userId).FirstOrDefault(a => a.IsPrimary);
        }

        public ICollection<EpgpTransaction> GetTransactions(string alias)
        {
            return _dbContext.Aliases
                .Include(a => a.Transactions)
                .FirstOrDefault(a => a.Name.ToUpper().Contains(alias.ToUpper()))?
                .Transactions.ToList();
        }

        public ICollection<EpgpTransaction> GetTransactions()
        {
            return _dbContext.EpgpTransactions.ToList();
        }

        public void PostTransaction(EpgpTransaction transaction, bool updateAlias = true)
        {
            _dbContext.EpgpTransactions.Add(transaction);
            if (!updateAlias) return;
            var alias = GetAlias(transaction.AliasId);
            switch (transaction.TransactionType)
            {
                case TransactionType.EpAutomated:
                case TransactionType.EpManual:
                case TransactionType.EpDecay:
                    alias.EffortPoints += transaction.Value;
                    break;
                case TransactionType.GpFromGear:
                case TransactionType.GpManual:
                case TransactionType.GpDecay:
                    alias.GearPoints += transaction.Value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}