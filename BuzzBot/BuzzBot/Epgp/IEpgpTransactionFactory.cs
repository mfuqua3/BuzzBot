using BuzzBotData.Data;

namespace BuzzBot.Epgp
{
    public interface IEpgpTransactionFactory
    {
        EpgpTransaction GetTransaction(EpgpAlias alias, int value, string memo, TransactionType transactionType);
    }
}