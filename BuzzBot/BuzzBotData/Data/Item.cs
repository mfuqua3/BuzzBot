﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace BuzzBotData.Data
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public int QualityValue { get; set; }
        public string Quality { get; set; }
        public int Class { get; set; }
        public int? Subclass { get; set; }
        public int ItemLevel { get; set; }
        public int InventorySlot { get; set; }

        public string RuName { get; set; }
        public string DeName { get; set; }
        public string FrName { get; set; }
        public string CnName { get; set; }
        public string ItName { get; set; }
        public string EsName { get; set; }
        public string PtName { get; set; }
        public string KoName { get; set; }

        [JsonIgnore]
        public List<BagSlot> Slots { get; set; }

        [JsonIgnore]
        public List<Bag> Bags { get; set; }
        [JsonIgnore]
        public List<RaidItem> RaidItems { get; set; }

        [JsonIgnore]
        public List<ItemRequestDetail> ItemRequestDetails { get; set; }

        [JsonIgnore]
        public List<TransactionDetail> TransactionDetails { get; set; }
        [JsonIgnore]
        public List<LiveItemData> LiveItemData { get; set; }
    }
}
