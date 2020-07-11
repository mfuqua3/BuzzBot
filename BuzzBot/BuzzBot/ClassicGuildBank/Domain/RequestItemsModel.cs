using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BuzzBot.ClassicGuildBank.Domain
{
    public class RequestItemsModel
    {
        #region Properties

        public string CharacterName
        { 
            get; 
            set; 
        }

        public int Gold
        {
            get;
            set;
        }

        [Required]
        public Guid GuildId
        {
            get;
            set;
        }

        private List<RequestItemModel> _requestItemModels;
        public List<RequestItemModel> RequestItemModels
        {
            get
            {
                if (_requestItemModels == null)
                    _requestItemModels = new List<RequestItemModel>();

                return _requestItemModels;
            }
            set
            {
                _requestItemModels = value;
            }
        }

        #endregion
    }
}
