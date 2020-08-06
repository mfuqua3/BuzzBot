using System;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IAliasEventAlerter
    {
        void RaisePrimaryAliasChanged(AliasChangeEventArgs args);
        void RaiseActiveAliasChanged(AliasChangeEventArgs args);
        void RaiseAliasAdded(EpgpAlias addedAlias);
        /// <summary>
        /// Raised when a primary alias is changed
        /// </summary>
        event EventHandler<AliasChangeEventArgs> PrimaryAliasChanged;
        /// <summary>
        /// Raised when an active alias is changed
        /// </summary>
        event EventHandler<AliasChangeEventArgs> ActiveAliasChanged;

        event EventHandler<EpgpAlias> AliasAdded;
    }
}