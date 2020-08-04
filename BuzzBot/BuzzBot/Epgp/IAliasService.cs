using System;
using System.Collections.Generic;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IAliasService
    {
        /// <summary>
        /// Gets the alias for the specified user ID that is currently being used for application alias activities
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        EpgpAlias GetActiveAlias(ulong userId);
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

        EpgpAlias GetAlias(string aliasName);

        void AddAlias(EpgpAlias alias);
        /// <summary>
        /// Raised when a primary alias is changed
        /// </summary>
        event EventHandler<AliasChangeEventArgs> PrimaryAliasChanged;
        /// <summary>
        /// Raised when an active alias is changed
        /// </summary>
        event EventHandler<AliasChangeEventArgs> ActiveAliasChanged;

        event EventHandler<EpgpAlias> AliasAdded;
        EpgpAlias GetAlias(Guid aliasId);
        void DeleteAlias(string aliasName);
    }

    public class AliasChangeEventArgs : EventArgs
    {
        public AliasChangeEventArgs(ulong user, EpgpAlias oldValue, EpgpAlias newValue)
        {
            User = user;
            OldValue = oldValue;
            NewValue = newValue;
            if (newValue.UserId != user || oldValue.UserId != user)
                throw new ArgumentException("Alias user ID must match provided user");
        }

        public ulong User { get; }
        public EpgpAlias OldValue { get; }
        public EpgpAlias NewValue { get; }
    }
}