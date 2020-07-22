using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BuzzBotData.Data
{
    public class BuzzBotDbContext : DbContext
    {
        private readonly string _connectionString;
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Bag> Bags { get; set; }
        public DbSet<BagSlot> BagSlots { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<GuildUser> GuildUsers { get; set; }
        public DbSet<EpgpAlias> Aliases { get; set; }
        public DbSet<EpgpTransaction> EpgpTransactions { get; set; }

        public BuzzBotDbContext(IConfiguration configuration)
        {
            _connectionString = configuration["connectionString"];
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connectionString);
            optionsBuilder.EnableSensitiveDataLogging();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                entity.ToTable("Item");
                entity.HasKey(i => i.Id);
            });

            modelBuilder.Entity<BagSlot>(entity =>
            {
                entity.ToTable("BagSlot");
                entity.HasKey(bs => new { bs.BagId, bs.SlotId });
                entity.HasOne<Item>(bs => bs.Item)
                    .WithMany(i => i.Slots)
                    .HasForeignKey(bs => bs.ItemId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<Bag>(bs => bs.Bag)
                    .WithMany(b => b.BagSlots)
                    .HasForeignKey(bs => bs.BagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Bag>(entity =>
            {
                entity.ToTable("Bag");
                entity.HasKey(b => b.Id);
                entity.HasOne<Item>(b => b.BagItem)
                    .WithMany(i => i.Bags)
                    .HasForeignKey(b => b.ItemId)
                    .IsRequired(false);

                entity.HasOne<Character>(b => b.Character)
                    .WithMany(c => c.Bags)
                    .HasForeignKey(b => b.CharacterId)
                    .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.ToTable("Character");
                entity.HasKey(c => c.Id);

                entity.HasOne(c => c.Guild)
                    .WithMany(g => g.Characters)
                    .HasForeignKey(c => c.GuildId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.ToTable("Guild");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.PublicLinkEnabled).HasDefaultValue(false);
            });
        }
    }
}