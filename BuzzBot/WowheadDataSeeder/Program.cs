using System;
using System.Collections.Generic;
using System.IO;
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
        const String itemsFileLocation = "items.csv";

        static async Task Main(string[] args)
        {
            //Get the file as a long string.
            string itemsString = File.ReadAllText(itemsFileLocation);

            //Handle any manual corrections we may need to do.
            itemsString = itemsString.Replace("entry,name\r\n", "");
            itemsString = itemsString.Replace("Monster - Item, Lantern - Round", "Monster - Item Lantern - Round");
            itemsString = itemsString.Replace("Thunderfury, Blessed Blade of the Windseeker", "Thunderfury Blessed Blade of the Windseeker");
            itemsString = itemsString.Replace("Zin'rokh, Destroyer of Worlds", "Zin'rokh Destroyer of Worlds");
            itemsString = itemsString.Replace("\r\n", ",");

            //Split the string on the ','
            string[] itemsArray = itemsString.Split(',');

            //Create a dictionary to store all of the determined values.
            List<Item> itemsList = new List<Item>();
            for (int i = 0; i < itemsArray.Length; i = i + 2)
            {
                //Add to the convertedItemsArray the list of 
                itemsList.Add(new Item() {
                    Entry = int.TryParse(itemsArray[i], out int iEntry) ? iEntry : 0,
                    Name = (itemsArray.Length < i + 1) ? "N/a" : itemsArray[i + 1],
                });
            }

            //instantiate the WowheadClient
            var wowheadClient = new WowheadClient();
            var db = new GuildBankDbContext();

            db.Database.EnsureCreated();
            foreach (var item in itemsList)
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

            //Save the database.
            db.SaveChanges();
        }
    }
}
