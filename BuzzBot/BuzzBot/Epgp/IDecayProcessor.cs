namespace BuzzBot.Epgp
{
    public interface IDecayProcessor
    {
        void Initialize();
        void Decay(EpgpConfiguration config);
    }
}