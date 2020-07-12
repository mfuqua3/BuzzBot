﻿using System;

namespace BuzzBotData.Data
{
    public class TransactionDetail
    {
        #region Properties

        public Guid Id { get; set; }

        public int? ItemId { get; set; }

        public int Quantity { get; set; }

        public Guid TransactionId { get; set; }

        public Transaction Transaction { get; set; }

        public Item Item { get; set; } 
        
        #endregion
    }
}
