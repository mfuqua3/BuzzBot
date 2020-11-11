using System.Collections.Generic;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public class AhnQirajTempleItemMapper : TierTokenItemMapper
    {

        protected override Dictionary<int, HashSet<int>> MappedItemDictionary { get; } = new Dictionary<int, HashSet<int>>
        {
            {21232, new HashSet<int>{21242,21272,21244,21269}}, //Imperial Qiraji Armaments
            {21237,  new HashSet<int>{21273,21275,21268}}, //Imperial Qiraji Regalia
            {20932, new HashSet<int>{21388,21391,21338,21335,21344,21345,21355,21354,21373,21376} }, //Qiraji Bindings of Dominance
            {20928, new HashSet<int>{21333,21330,21359,21361,21349,21350,21365,21367}}, //Qiraji Bindings of Command
            {20930, new HashSet<int>{21387,21360,21353,21372,21366}}, //Veklors Diadem
            {20926, new HashSet<int>{21329,21337,21347,21348}}, //Veknilash's Circlet
            {20931, new HashSet<int>{21390,21336,21356,21375,21368} }, //Skin of the Great Sandworm
            {20927, new HashSet<int>{21332,21362,21346,21352} }, //Ouro's Intact Hide
            {20933, new HashSet<int>{21334,21343,21357,21351}  }, //Husk of the Old God
            {20929,new HashSet<int>{21389,21331,21364,21374,21370} }
        };

        protected override Dictionary<int, Class> ClassDictionary { get; } = new Dictionary<int, Class>
        {
            {21388, Class.Paladin },
            {21391, Class.Paladin },
            {21387, Class.Paladin },
            {21390, Class.Paladin },
            {21389, Class.Paladin },

            {21333, Class.Warrior },
            {21330, Class.Warrior },
            {21332, Class.Warrior },
            {21331, Class.Warrior },
            {21329, Class.Warrior },

            {21365, Class.Hunter },
            {21367, Class.Hunter },
            {21366, Class.Hunter },
            {21368, Class.Hunter },
            {21370, Class.Hunter },

            {21359, Class.Rogue },
            {21361, Class.Rogue },
            {21360, Class.Rogue },
            {21362, Class.Rogue },
            {21364, Class.Rogue },

            {21338, Class.Warlock },
            {21335, Class.Warlock },
            {21336, Class.Warlock },
            {21334, Class.Warlock },
            {21337, Class.Warlock },

            {21344, Class.Mage },
            {21345, Class.Mage },
            {21343, Class.Mage },
            {21346, Class.Mage },
            {21347, Class.Mage },

            {21349, Class.Priest },
            {21350, Class.Priest },
            {21351, Class.Priest },
            {21352, Class.Priest },
            {21348, Class.Priest },

            {21354, Class.Druid },
            {21355, Class.Druid },
            {21353, Class.Druid },
            {21356, Class.Druid },
            {21357, Class.Druid },

            {21373, Class.Shaman },
            {21376, Class.Shaman },
            {21372, Class.Shaman },
            {21375, Class.Shaman },
            {21374, Class.Shaman },
        };

        public AhnQirajTempleItemMapper(BuzzBotDbContext dbContext) : base(dbContext)
        {
        }
    }
}