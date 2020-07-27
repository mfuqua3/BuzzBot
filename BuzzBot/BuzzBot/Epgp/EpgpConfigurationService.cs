using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace BuzzBot.Epgp
{
    public class EpgpConfigurationService : IEpgpConfigurationService
    {
        private string _filePath;
        private EpgpConfiguration _configuration;
        public EpgpConfigurationService()
        {
            Initialize();
        }

        private void Initialize()
        {
            var fileDir = Directory.GetCurrentDirectory();
            _filePath = Path.Combine(fileDir, @"epgpconfig.json");
            if (!File.Exists(_filePath))
            {
                _configuration = new EpgpConfiguration
                {
                    DecayPercentage = 5,
                    DecayDayOfWeek = DayOfWeek.Monday,
                    EpMinimum = 1,
                    GpMinimum = 1,
                    Templates = new List<EpgpRaidTemplate>()
                };
                Save();
            }

            _configuration = Load();
        }
        public EpgpConfiguration GetConfiguration()
        {
            return _configuration;
        }

        public void UpdateConfig(int key, int value)
        {
            UpdateConfig(_configuration, key, value);
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddTemplate(EpgpRaidTemplate template)
        {
            if (_configuration.Templates.Any(t => template.TemplateId == t.TemplateId))
                throw new InvalidOperationException(
                    $"A template by the name {template.TemplateId} already exists. Use the update command or delete the existing template");
            _configuration.Templates.Add(template);
            Save();
        }

        public EpgpRaidTemplate GetTemplate([NotNull] string templateId)
        {
            var toReturn = _configuration.Templates.FirstOrDefault(t => t.TemplateId == templateId);
            if (toReturn == null)
                throw new ArgumentException($"No template by the name of {templateId}. Template names are case sensitive");
            return toReturn;
        }
        public void DeleteTemplate([NotNull] string templateId)
        {
            var toRemove = GetTemplate(templateId);
            _configuration.Templates.Remove(toRemove);
            Save();
        }

        public void UpdateTemplate([NotNull] string templateId, int key, int value)
        {
            var template = GetTemplate(templateId);
            UpdateConfig(template, key, value);
        }


        private void UpdateConfig(object configObject, int key, int value)
        {
            var property = GetConfigurationProperty(configObject, key);
            if (property.PropertyType != typeof(int))
            {
                throw new InvalidOperationException($"Invalid use of {nameof(UpdateConfig)}, {property.Name} is not of type {nameof(Int32)}");
            }
            property.SetValue(configObject, value);
            Save();
        }

        private PropertyInfo GetConfigurationProperty(object propertyObject, int key)
        {
            var properties = propertyObject.GetType().GetProperties()
                .Where(pi => Attribute.IsDefined(pi, typeof(ConfigurationKeyAttribute))).ToList();
            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<ConfigurationKeyAttribute>();
                if (attribute.Key == key) return propertyInfo;
            }
            throw new ArgumentException($"Unable to find a property in {propertyObject.GetType().Name} with configuration key {key}");
        }

        private void Save()
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_configuration, Formatting.Indented));
        }

        private EpgpConfiguration Load()
        {
            return JsonConvert.DeserializeObject<EpgpConfiguration>(File.ReadAllText(_filePath));
        }

        public event EventHandler ConfigurationChanged;
    }
}