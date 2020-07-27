﻿// <auto-generated />
using System;
using BuzzBotData.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BuzzBotData.Migrations
{
    [DbContext(typeof(BuzzBotDbContext))]
    partial class BuzzBotDbContexttModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.5");

            modelBuilder.Entity("BuzzBotData.Data.Bag", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BagContainerId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("CharacterId")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("isBank")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.HasIndex("ItemId");

                    b.ToTable("Bag");
                });

            modelBuilder.Entity("BuzzBotData.Data.BagSlot", b =>
                {
                    b.Property<Guid>("BagId")
                        .HasColumnType("TEXT");

                    b.Property<int>("SlotId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.HasKey("BagId", "SlotId");

                    b.HasIndex("ItemId");

                    b.ToTable("BagSlot");
                });

            modelBuilder.Entity("BuzzBotData.Data.Character", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Gold")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("GuildId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastUpdated")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("Character");
                });

            modelBuilder.Entity("BuzzBotData.Data.EpgpAlias", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Class")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EffortPoints")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GearPoints")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsActive")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Aliases");
                });

            modelBuilder.Entity("BuzzBotData.Data.EpgpTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("AliasId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Memo")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TransactionDateTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("TransactionType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Value")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AliasId");

                    b.ToTable("EpgpTransactions");
                });

            modelBuilder.Entity("BuzzBotData.Data.Guild", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("InviteUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PublicLinkEnabled")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(false);

                    b.Property<string>("PublicUrl")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Guild");
                });

            modelBuilder.Entity("BuzzBotData.Data.GuildUser", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("GuildUsers");
                });

            modelBuilder.Entity("BuzzBotData.Data.Item", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Class")
                        .HasColumnType("INTEGER");

                    b.Property<string>("CnName")
                        .HasColumnType("TEXT");

                    b.Property<string>("DeName")
                        .HasColumnType("TEXT");

                    b.Property<string>("EsName")
                        .HasColumnType("TEXT");

                    b.Property<string>("FrName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Icon")
                        .HasColumnType("TEXT");

                    b.Property<int>("InventorySlot")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ItName")
                        .HasColumnType("TEXT");

                    b.Property<int>("ItemLevel")
                        .HasColumnType("INTEGER");

                    b.Property<string>("KoName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("PtName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Quality")
                        .HasColumnType("TEXT");

                    b.Property<int>("QualityValue")
                        .HasColumnType("INTEGER");

                    b.Property<string>("RuName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Subclass")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Item");
                });

            modelBuilder.Entity("BuzzBotData.Data.ItemRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CharacterName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("DateRequested")
                        .HasColumnType("TEXT");

                    b.Property<int>("Gold")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("GuildId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Reason")
                        .HasColumnType("TEXT");

                    b.Property<string>("Status")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("ItemRequest");
                });

            modelBuilder.Entity("BuzzBotData.Data.ItemRequestDetail", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("ItemRequestId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.HasIndex("ItemRequestId");

                    b.ToTable("ItemRequestDetail");
                });

            modelBuilder.Entity("BuzzBotData.Data.Transaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CharacterName")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("GuildId")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Money")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("TransactionDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Type")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.ToTable("Transaction");
                });

            modelBuilder.Entity("BuzzBotData.Data.TransactionDetail", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int?>("ItemId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("TransactionId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.HasIndex("TransactionId");

                    b.ToTable("TransactionDetail");
                });

            modelBuilder.Entity("BuzzBotData.Data.Bag", b =>
                {
                    b.HasOne("BuzzBotData.Data.Character", "Character")
                        .WithMany("Bags")
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BuzzBotData.Data.Item", "BagItem")
                        .WithMany("Bags")
                        .HasForeignKey("ItemId");
                });

            modelBuilder.Entity("BuzzBotData.Data.BagSlot", b =>
                {
                    b.HasOne("BuzzBotData.Data.Bag", "Bag")
                        .WithMany("BagSlots")
                        .HasForeignKey("BagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BuzzBotData.Data.Item", "Item")
                        .WithMany("Slots")
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.SetNull);
                });

            modelBuilder.Entity("BuzzBotData.Data.Character", b =>
                {
                    b.HasOne("BuzzBotData.Data.Guild", "Guild")
                        .WithMany("Characters")
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.EpgpAlias", b =>
                {
                    b.HasOne("BuzzBotData.Data.GuildUser", "User")
                        .WithMany("Aliases")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.EpgpTransaction", b =>
                {
                    b.HasOne("BuzzBotData.Data.EpgpAlias", "Alias")
                        .WithMany("Transactions")
                        .HasForeignKey("AliasId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.ItemRequest", b =>
                {
                    b.HasOne("BuzzBotData.Data.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.ItemRequestDetail", b =>
                {
                    b.HasOne("BuzzBotData.Data.Item", "Item")
                        .WithMany("ItemRequestDetails")
                        .HasForeignKey("ItemId");

                    b.HasOne("BuzzBotData.Data.ItemRequest", "ItemRequest")
                        .WithMany("Details")
                        .HasForeignKey("ItemRequestId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.Transaction", b =>
                {
                    b.HasOne("BuzzBotData.Data.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BuzzBotData.Data.TransactionDetail", b =>
                {
                    b.HasOne("BuzzBotData.Data.Item", "Item")
                        .WithMany("TransactionDetails")
                        .HasForeignKey("ItemId");

                    b.HasOne("BuzzBotData.Data.Transaction", "Transaction")
                        .WithMany("TransactionDetails")
                        .HasForeignKey("TransactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
