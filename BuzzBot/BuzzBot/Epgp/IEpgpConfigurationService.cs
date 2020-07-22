namespace BuzzBot.Epgp
{
    public interface IEpgpConfigurationService
    {
        EpgpConfiguration GetConfiguration();
        void UpdateConfig(int key, int value);
        void AddTemplate(EpgpRaidTemplate template);
        void DeleteTemplate(string templateId);
        void UpdateTemplate(string templateId, int key, int value);
    }
}