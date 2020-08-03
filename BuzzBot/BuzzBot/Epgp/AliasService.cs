using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class AliasService : IAliasService
    {
        private readonly EpgpRepository _epgpRepository;

        public AliasService(EpgpRepository epgpRepository)
        {
            _epgpRepository = epgpRepository;
        }
        public EpgpAlias GetActiveAlias(ulong userId)
        {
            var aliases = GetAliases(userId);
            var activeAlias = aliases.FirstOrDefault(a => a.IsActive);
            if (activeAlias == null)
            {
                activeAlias = GetPrimaryAlias(userId);
                activeAlias.IsActive = true;
                _epgpRepository.Save();
            }

            return activeAlias;
        }

        public EpgpAlias GetPrimaryAlias(ulong userId)
        {
            var aliases = GetAliases(userId);
            if(!aliases.Any(a=>a.IsPrimary))
                throw new InvalidOperationException("No primary alias could be found for user.");
            return aliases.First(a => a.IsPrimary);
        }

        public void SetActiveAlias(ulong userId, string aliasName)
        {
            var currentActive = GetActiveAlias(userId);
            if (currentActive.Name.Equals(aliasName)) return;
            currentActive.IsActive = false;
            var aliases = GetAliases(userId);
            if(!aliases.Any(a=>a.Name.Equals(aliasName)))
                throw new ArgumentException($"No alias named {aliasName} could be found for the specified user.");
            var newActive = aliases.First(a => a.Name.Equals(aliasName));
            newActive.IsActive = true;
            _epgpRepository.Save();
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
            _epgpRepository.Save();
            PrimaryAliasChanged?.Invoke(this, new AliasChangeEventArgs(userId, currentPrimary, newPrimary));
        }

        public List<EpgpAlias> GetAliases(ulong userId)
        {
            return _epgpRepository.GetAliasesForUser(userId).ToList();
        }

        public void AddAlias(EpgpAlias alias)
        {
            _epgpRepository.AddAlias(alias);
            _epgpRepository.Save();
            AliasAdded?.Invoke(this, alias);
        }

        public event EventHandler<AliasChangeEventArgs> PrimaryAliasChanged;
        public event EventHandler<AliasChangeEventArgs> ActiveAliasChanged;
        public event EventHandler<EpgpAlias> AliasAdded;
    }
}