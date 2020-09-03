using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore.Internal;

namespace BuzzBot.Epgp
{
    public interface IAliasService
    {
        /// <summary>
        /// Gets the alias for the specified user ID that is currently being used for application alias activities
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        IEnumerable<EpgpAlias> GetActiveAliases(ulong userId);
        /// <summary>
        /// Gets the alias for the specified user that has been designated the "main" or "primary"
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        EpgpAlias GetPrimaryAlias(ulong userId);
        /// <summary>
        /// Sets the alias for the specified user ID that is currently being used for application alias activities
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="aliasName"></param>
        void SetActiveAlias(ulong userId, string aliasName);
        /// <summary>
        /// Sets the alias for the specified user that has been designated the "main" or "primary"
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="aliasName"></param>
        void SetPrimaryAlias(ulong userId, string aliasName);
        /// <summary>
        /// Get all aliases registered to the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        List<EpgpAlias> GetAliases(ulong userId);

        void AddAlias(EpgpAlias alias);
        void DeleteAlias(string aliasName);
        EpgpAlias GetAlias(Guid aliasId);
        EpgpAlias GetAlias(string aliasName);

        void SetActiveAlias(ulong userId, string aliasName,
            Action<IAliasConfiguration> configurationOptions);
    }

    public class AliasChangeEventArgs : EventArgs
    {
        public AliasChangeEventArgs(ulong user, ICollection<EpgpAlias> oldValues, ICollection<EpgpAlias> newValues)
        {
            User = user;
            OldValues = oldValues;
            NewValues = newValues;
            if (newValues.Any(nv => nv.UserId != user) || oldValues.Any(ov => ov.UserId != user))
                throw new ArgumentException("Alias user ID must match provided user");
        }

        public ulong User { get; }
        public ICollection<EpgpAlias> OldValues { get; }
        public ICollection<EpgpAlias> NewValues { get; }
    }
}