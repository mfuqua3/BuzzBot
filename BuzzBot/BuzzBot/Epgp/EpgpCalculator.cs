using System;
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
            return (int) Math.Ceiling((double) totalCopper / CopperPerGp);
        }

        public double CalculateItem(Item item, bool isHunter, bool isOffhand)
        {
            var iLevel = item.ItemLevel;
            var quality = item.QualityValue;
            var slot = item.InventorySlot;
            return Math.Pow(GetItemValue(iLevel, quality), 2) * 0.04 * GetSlotValue(slot, isHunter, isOffhand);
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
                        return 0.5;
                    case 15:
                        return 1.5;
                }
            switch (slot)
            {
                case 17: //2h weapon
                    return 2;
                case 13: //1H weapon
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