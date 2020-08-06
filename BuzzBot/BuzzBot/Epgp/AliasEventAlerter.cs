using System;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class AliasEventAlerter : IAliasEventAlerter
    {
        public void RaisePrimaryAliasChanged(AliasChangeEventArgs args)
        {
            PrimaryAliasChanged?.Invoke(this, args);
        }

        public void RaiseActiveAliasChanged(AliasChangeEventArgs args)
        {
            ActiveAliasChanged?.Invoke(this, args);
        }

        public void RaiseAliasAdded(EpgpAlias addedAlias)
        {
            AliasAdded?.Invoke(this, addedAlias);
        }

        public event EventHandler<AliasChangeEventArgs> PrimaryAliasChanged;
        public event EventHandler<AliasChangeEventArgs> ActiveAliasChanged;
        public event EventHandler<EpgpAlias> AliasAdded;
    }
}