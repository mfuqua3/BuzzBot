using System;

namespace BuzzBot.Epgp
{
    public interface IRaidFactory
    {
        EpgpRaid CreateNew(string templateId);
    }
}