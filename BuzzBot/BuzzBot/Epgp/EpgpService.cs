using System;
using BuzzBotData.Data;
using BuzzBotData.Repositories;

namespace BuzzBot.Epgp
{
    public class EpgpService
    {
        private readonly EpgpRepository _epgpRepository;
        public const string EpFlag = "-ep";
        public const string GpFlag = "-gp";

        public EpgpService(EpgpRepository epgpRepository)
        {
            _epgpRepository = epgpRepository;
        }

        public void Decay(int decayPercent)
            => Decay(decayPercent, null);

        public void Decay(int decayPercent, string epgpFlag)
        {
            var asPercent = (double)decayPercent / 100;
            var aliases = _epgpRepository.GetAliases();
            foreach (var alias in aliases)
            {

                var epDecay = (int)Math.Round(alias.EffortPoints * asPercent, MidpointRounding.AwayFromZero);
                var gpDecay = (int)Math.Round(alias.GearPoints * asPercent, MidpointRounding.AwayFromZero);
                var epTransaction = new EpgpTransaction
                {
                    Id = Guid.NewGuid(),
                    AliasId = alias.Id,
                    Memo = $"{decayPercent}% Decay",
                    TransactionType = TransactionType.EpDecay,
                    Value = -epDecay
                };
                var gpTransaction = new EpgpTransaction
                {
                    Id = Guid.NewGuid(),
                    AliasId = alias.Id,
                    Memo = $"{decayPercent}% Decay",
                    TransactionType = TransactionType.GpDecay,
                    Value = -gpDecay
                };
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(EpFlag))
                    _epgpRepository.PostTransaction(epTransaction);
                if (string.IsNullOrWhiteSpace(epgpFlag) || epgpFlag.Equals(GpFlag))
                    _epgpRepository.PostTransaction(gpTransaction);
            }
            _epgpRepository.Save();
        }
    }
}