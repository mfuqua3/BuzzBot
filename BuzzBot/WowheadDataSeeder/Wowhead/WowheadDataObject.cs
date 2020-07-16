using System.Xml.Serialization;

namespace WowheadDataSeeder.Wowhead
{

    [XmlRoot(ElementName = "wowhead")]
    public class WowheadDataObject
    {
        [XmlElement(ElementName = "item")]
        public XmlWarcraftItem Item { get; set; }
    }
}