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

        public void DeleteGuildUser(ulong id)
        {
            var aliases = GetAliasesForUser(id);
            foreach (var alias in aliases)
            {
                var transactions = _dbContext.EpgpTransactions.Where(t => t.AliasId == alias.Id);
                foreach (var transaction in transactions)
                {
                    _dbContext.EpgpTransactions.Remove(transaction);
                }

                _dbContext.Aliases.Remove(alias);
            }

            var user = _dbContext.GuildUsers.Find(id);
            _dbContext.GuildUsers.Remove(user);
            Save();
        }

        public void DeleteAlias(string aliasName)
        {
            var alias = _dbContext.Aliases.ToList()
                .FirstOrDefault(a => a.Name.Equals(aliasName, StringComparison.InvariantCultureIgnoreCase));
            if (alias == null) return;
            var transactions = _dbContext.EpgpTransactions.Where(t => t.AliasId == alias.Id);
            foreach (var transaction in transactions)
            {
                _dbContext.EpgpTransactions.Remove(transaction);
            }

            _dbContext.Aliases.Remove(alias);
            Save();
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
            var aliases = _dbContext.Aliases.Where(a => a.Name.ToUpper().Contains(name.ToUpper())).ToList();
            return aliases.Any(a => a.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)) ? 
                aliases.FirstOrDefault(a => a.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)) : 
                aliases.FirstOrDefault();
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