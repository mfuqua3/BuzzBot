using System.ComponentModel.DataAnnotations;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class RequestItemModel
    {
        #region Properties

        [Required]
        public int ItemId
        {
            get;
            set;
        }
        
        public int Quantity
        {
            get;
            set;
        }

        #endregion
    }
}
