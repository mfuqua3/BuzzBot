using System;
using System.Collections.Generic;

namespace BuzzBot.Discord.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(this Dictionary<TKey, TValue> source,
            params IDictionary<TKey, TValue>[] targets)
        {
            foreach (var target in targets)
            {
                foreach (var keyValuePair in target)
                {
                    if (source.ContainsKey(keyValuePair.Key))
                    {
                        throw new InvalidOperationException($"Unable to complete merge operation. Duplicate occurrence of key \"{keyValuePair.Key.ToString()}\"");
                    }
                    source.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            return source;
        }
    }
}