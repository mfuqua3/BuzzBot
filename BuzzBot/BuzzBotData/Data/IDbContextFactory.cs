using Microsoft.Extensions.Configuration;

namespace BuzzBotData.Data
{
    public interface IDbContextFactory
    {
        BuzzBotDbContext GetNew();
    }

    public class DbContextFactory : IDbContextFactory
    {
        private readonly IConfiguration _configuration;

        public DbContextFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public BuzzBotDbContext GetNew() => new BuzzBotDbContext(_configuration);
    }
}