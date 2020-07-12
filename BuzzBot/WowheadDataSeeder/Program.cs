using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BuzzBotData.Data;
using FileHelpers;
using WowheadDataSeeder.Properties;
using WowheadDataSeeder.Wowhead;

namespace WowheadDataSeeder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var engine = new FileHelperEngine<Item>();
            var items = engine.ReadFile("items.csv").GroupBy(itm=>itm.Entry).Select(itm=>itm.First());
            var wowheadClient = new WowheadClient();
            var db = new GuildBankDbContext();
            db.Database.EnsureCreated();
            foreach (var item in items)
            {
                var wowheadItem = await wowheadClient.Get(item.Entry.ToString());
                if (wowheadItem.Item == null) continue;
                Console.WriteLine($"Adding {item.Name} to database");
                var dbItem = new BuzzBotData.Data.Item
                {
                    Id = item.Entry,
                    Name = wowheadItem.Item.Name,
                    Quality = wowheadItem.Item.Quality.Text,
                    Icon = wowheadItem.Item.Icon.IconName,
                    Class = Convert.ToInt32(wowheadItem.Item.Class.Id),
                    Subclass = Convert.ToInt32(wowheadItem.Item.Subclass.Id)
                };
                db.Items.Add(dbItem);
            }

            ;
            db.SaveChanges();
        }
    }
}
