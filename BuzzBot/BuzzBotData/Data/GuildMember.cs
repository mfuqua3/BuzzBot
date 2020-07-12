using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BuzzBotData.Data
{
    public class GuildMember
    {
        public string UserId { get; set; }

        [JsonIgnore]
        public Guid GuildId { get; set; }

        [NotMapped]
        [JsonProperty("guildId")]
        public string _GuildId { get => GuildId.ToString(); set => GuildId = new Guid(value); }

        public string DisplayName { get; set; }

        public bool CanUpload { get; set; }

        [JsonIgnore]
        public Guild Guild { get; set; }
    }
}
