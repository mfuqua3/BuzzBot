using System;
using System.Threading.Tasks;

namespace BuzzBot.Epgp
{
    public interface IRaidFactory
    {
        Task<EpgpRaid> CreateNew(string templateId);
    }
}