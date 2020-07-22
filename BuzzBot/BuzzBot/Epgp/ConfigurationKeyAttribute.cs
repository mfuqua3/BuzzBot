using System;

namespace BuzzBot.Epgp
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ConfigurationKeyAttribute : Attribute
    {
        public ConfigurationKeyAttribute(int key)
        {
            Key = key;
        }

        public int Key { get; }
    }
}