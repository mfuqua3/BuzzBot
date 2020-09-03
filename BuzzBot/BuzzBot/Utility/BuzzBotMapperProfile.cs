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

            CreateMap<EpgpAlias, EpgpAliasViewModel>()
                .ReverseMap();
        }
    }
}