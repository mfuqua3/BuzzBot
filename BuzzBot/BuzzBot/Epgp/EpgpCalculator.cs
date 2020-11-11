using System;
using System.Collections.Generic;
using BuzzBot.Wowhead;
using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IEpgpCalculator
    {
        int ConvertGpFromGold(int totalCopper);
        double CalculateItem(Item item, bool isHunter, bool isOffhand);
    }

    public class EpgpCalculator : IEpgpCalculator
    {
        private const int CopperPerGp = 100000;
        public int ConvertGpFromGold(int totalCopper)
        {
            return (int)Math.Ceiling((double)totalCopper / CopperPerGp);
        }

        public double CalculateItem(Item item, bool isHunter, bool isOffhand)
        {
            if (TryParseTierToken(item, out var gpValue))
            {
                return gpValue;
            }
            var iLevel = item.ItemLevel;
            var quality = item.QualityValue;
            var slot = item.InventorySlot;
            return Math.Pow(GetItemValue(iLevel, quality), 2) * 0.04 * GetSlotValue(slot, isHunter, isOffhand);
        }

        private class TokenData
        {
            public TokenData(int level, int slot, int quality)
            {
                ILevel = level;
                Slot = slot;
                Quality = quality;
            }

            public int ILevel { get; set; }
            public int Slot { get; set; }
            public int Quality { get; set; }
        }

        private Dictionary<int, TokenData> _naxxTokenDatas = new Dictionary<int, TokenData>
        {
            //Chest
            {22351,new TokenData(88,5,4)},
            {22350,new TokenData(88,5,4)},
            {22349,new TokenData(88,5,4)},
            //Legs
            {22366,new TokenData(88,7,4)},
            {22359,new TokenData(88,7,4)},
            {22352,new TokenData(88,7,4)},
            //Head
            {22367,new TokenData(88,1,4)},
            {22360,new TokenData(88,1,4)},
            {22353,new TokenData(88,1,4)},
            //Arms
            {22369,new TokenData(88,9,4)},
            {22362,new TokenData(88,9,4)},
            {22355,new TokenData(88,9,4)},
            //Hands
            {22371,new TokenData(88,10,4)},
            {22364,new TokenData(88,10,4)},
            {22357,new TokenData(88,10,4)},
            //Feet
            {22372,new TokenData(88,8,4)},
            {22365,new TokenData(88,8,4)},
            {22358,new TokenData(88,8,4)},
            //Belt
            {22370,new TokenData(88,6,4)},
            {22363,new TokenData(88,6,4)},
            {22356,new TokenData(88,6,4)},
            //Shoulders
            {22368,new TokenData(88,3,4)},
            {22361,new TokenData(88,3,4)},
            {22354,new TokenData(88,3,4)},
        };

        private bool TryParseTierToken(Item item, out double gp)
        {
            gp = 0;
            if (!_naxxTokenDatas.ContainsKey(item.Id)) return false;
            var tokenData = _naxxTokenDatas[item.Id];
            gp = Math.Pow(GetItemValue(tokenData.ILevel, tokenData.Quality), 2) * 0.04 * GetSlotValue(tokenData.Slot, false, false);
            return true;
        }

        private double GetItemValue(int iLevel, int quality)
        {
            switch (quality)
            {
                case 2:
                    return (double)(iLevel - 4) / 2;
                case 3:
                    return (double)(iLevel - 1.84) / 1.6;
                case 4:
                    return (double)(iLevel - 1.3) / 1.3;
                case 5:
                    return (double)iLevel;
                default:
                    return 0;
            }
        }

        private double GetSlotValue(int slot, bool hunter, bool isOffhand)
        {
            if (isOffhand && slot == 13)
                return 1;
            if (hunter)
                switch (slot)
                {
                    case 17:
                        return 1;
                    case 13:
                    case 21:
                        return 0.5;
                    case 15:
                        return 1.5;
                }
            switch (slot)
            {
                case 17: //2h weapon
                    return 2;
                case 13: //1H weapon
                case 21: //"Main Hand" weapon
                    return 1.5;
                case 3: //Shoulders
                case 12: //Trinket
                case 6: //Waist
                case 8: //Boots
                case 10: //Hands
                    return 0.75;
                case 14: //Shield
                case 15: //Ranged
                case 9: //Wrist
                case 2: //Neck
                case 16: //Back
                case 11: //Finger
                case 23: //Offhand
                case 28: //Idol
                    return 0.5;
                case 1: //Helm
                case 5: //Chest
                case 7: //Legs
                default:
                    return 1;
            }
        }
    }
}