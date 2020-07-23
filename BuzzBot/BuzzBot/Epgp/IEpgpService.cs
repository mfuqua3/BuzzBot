using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IEpgpService
    {
        void Ep(string aliasName, int value, string memo, TransactionType type = TransactionType.EpManual);
        void Gp(string aliasName, int value, string memo, TransactionType type = TransactionType.GpManual);
        void Decay(int decayPercent);
        void Decay(int decayPercent, string epgpFlag);
    }
}