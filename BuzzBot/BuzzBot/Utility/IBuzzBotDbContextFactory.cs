using BuzzBotData.Data;

namespace BuzzBot.Utility
{
    public interface IBuzzBotDbContextFactory
    {
        BuzzBotDbContext GetNew();
    }
}