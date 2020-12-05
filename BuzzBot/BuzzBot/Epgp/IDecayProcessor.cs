namespace BuzzBot.Epgp
{
    public interface IDecayProcessor
    {
        void Initialize();
        void Decay(EpgpConfiguration config);
        void Decay(int epDecayPercent, int gpDecayPercent);
    }
}