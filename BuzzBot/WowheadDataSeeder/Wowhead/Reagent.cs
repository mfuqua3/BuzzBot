using System.Xml.Serialization;

namespace WowheadDataSeeder.Wowhead
{
    [XmlRoot(ElementName = "reagent")]
    public class Reagent
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
        [XmlAttribute(AttributeName = "quality")]
        public string Quality { get; set; }
        [XmlAttribute(AttributeName = "icon")]
        public string Icon { get; set; }
        [XmlAttribute(AttributeName = "count")]
        public string Count { get; set; }
    }
}