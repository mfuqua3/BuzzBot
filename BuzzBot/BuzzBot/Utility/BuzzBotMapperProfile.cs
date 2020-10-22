using System;
using AutoMapper;
using BuzzBot.Discord.Extensions;
using BuzzBot.Epgp;
using BuzzBot.Models;
using BuzzBotData.Data;

namespace BuzzBot.Utility
{
    public class BuzzBotMapperProfile:Profile
    {
        public BuzzBotMapperProfile()
        {
            CreateMap<Class, WowClass>()
                .ConvertUsing(map => map.ToWowClass());

            CreateMap<WowClass, Class>()
                .ConvertUsing(map => map.ToDomainClass());

            CreateMap<RaidItem, LootCsvRecord>()
                .ForMember(csv => csv.TransactionDateTime, opt => opt.MapFrom(ri => ri.Transaction.TransactionDateTime))
                .ForMember(csv => csv.TransactionId, opt => opt.MapFrom(ri => ri.TransactionId.ToString("N")))
                .ForMember(csv => csv.ItemId, opt => opt.MapFrom(ri => ri.ItemId))
                .ForMember(csv => csv.ItemName, opt => opt.MapFrom(ri => ri.Item.Name))
                .ForMember(csv => csv.UserAlias, opt => opt.MapFrom(ri => ri.AwardedAlias.Name))
                .ForMember(csv => csv.RaidEventId, opt => opt.MapFrom(ri => ri.RaidId.ToString("N")))
                .ForAllOtherMembers(opt=>opt.Ignore());

            CreateMap<EpgpTransaction, TransactionCsvRecord>()
                .ForMember(csv => csv.Id, opt => opt.MapFrom(t => t.Id))
                .ForMember(csv => csv.TransactionDateTime, opt => opt.MapFrom(t => t.TransactionDateTime))
                .ForMember(csv => csv.DiscordUserId, opt => opt.MapFrom(t => t.Alias.UserId))
                .ForMember(csv => csv.CharacterName, opt => opt.MapFrom(t => t.Alias.Name))
                .ForMember(csv => csv.TransactionType, opt => opt.MapFrom(t => t.TransactionType))
                .ForMember(csv => csv.Value, opt => opt.MapFrom(t => t.Value))
                .ForMember(csv => csv.Memo, opt => opt.MapFrom(t => t.Memo));

            CreateMap<EpgpAlias, EpgpAliasViewModel>()
                .ReverseMap();
        }
    }
}