using System;
using System.Collections.Generic;
using System.Linq;
using BuzzBotData.Data;

namespace BuzzBotData.Repositories
{
    public class ItemRepository
    {
        private readonly BuzzBotDbContext _dbContext;

        public ItemRepository(BuzzBotDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public ICollection<Item> GetItems()
        {
            return _dbContext.Items.ToList();
        }

        public List<Item> GetItems(string queryString)
        {
            return GetItems()
                .Where(itm => itm.Name.Contains(queryString, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }

        public void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}