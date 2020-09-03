using System;
using BuzzBot.Epgp;

namespace BuzzBot.Models
{
    public class EpgpAliasViewModel
    {
        public Guid Id { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public WowClass Class { get; set; }
        public ulong UserId { get; set; }
    }
}