namespace BuzzBot.Epgp
{
    public class EpgpRaidTemplate
    {
        [ConfigurationKey(0)]
        public string TemplateId { get; set; }
        [ConfigurationKey(1)]
        public int RaidCapacity { get; set; }
        [ConfigurationKey(2)]
        public int StartBonus { get; set; }
        [ConfigurationKey(3)]
        public int EndBonus { get; set; }
        [ConfigurationKey(4)]
        public int TimeBonus { get; set; }
        [ConfigurationKey(5)]
        public int TimeBonusDurationMinutes { get; set; }
        [ConfigurationKey(6)]
        public int RaidDurationMinutes { get; set; }
        [ConfigurationKey(7)]
        public int SignUpDurationMinutes { get; set; }
    }
}