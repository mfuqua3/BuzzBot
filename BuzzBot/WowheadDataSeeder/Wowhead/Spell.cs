using System.Collections.Generic;
using System.Xml.Serialization;

namespace WowheadDataSeeder.Wowhead
{
    [XmlRoot(ElementName = "spell")]
    public class Spell
    {
        [XmlElement(ElementName = "reagent")]
        public List<Reagent> Reagent { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "icon")]
        public string Icon { get; set; }
        [XmlAttribute(AttributeName = "minCount")]
        public string MinCount { get; set; }
        [XmlAttribute(AttributeName = "maxCount")]
        public string MaxCount { get; set; }
    }
}