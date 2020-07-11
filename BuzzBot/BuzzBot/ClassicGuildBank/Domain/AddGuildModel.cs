using System.ComponentModel.DataAnnotations;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class AddGuildModel
    {
        #region Properties

        [Required]
        public string GuildName
        {
            get;
            set;
        }

        #endregion
    }
}
