﻿using System;

namespace BuzzBotData.Data
{
    public class ItemRequestDetail
    {
        public Guid Id { get; set; }
        
        public int? ItemId { get; set; }
        
        public int Quantity { get; set; }
        
        public Guid ItemRequestId { get; set; }

        public ItemRequest ItemRequest { get; set; }

        public Item Item { get; set; }
    }
}
