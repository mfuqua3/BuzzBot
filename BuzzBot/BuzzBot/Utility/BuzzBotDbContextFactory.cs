using BuzzBotData.Data;
using Microsoft.Extensions.Configuration;

namespace BuzzBot.Utility
{
    public class BuzzBotDbContextFactory : IBuzzBotDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public BuzzBotDbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BuzzBotDbContext GetNew()
        {
            return new BuzzBotDbContext(_configuration);
        }
    }
}