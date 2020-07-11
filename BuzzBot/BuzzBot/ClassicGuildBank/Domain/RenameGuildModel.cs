using System;
using System.ComponentModel.DataAnnotations;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class RenameGuildModel
    {
        #region Properties

        [Required]
        public Guid GuildId
        {
            get;
            set;
        }

        [Required]
        public string GuildName
        {
            get;
            set;
        }

        #endregion
    }
}
