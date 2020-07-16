using System.Xml.Serialization;

namespace WowheadDataSeeder.Wowhead
{
    [XmlRoot(ElementName = "createdBy")]
    public class CreatedBy
    {
        [XmlElement(ElementName = "spell")]
        public Spell Spell { get; set; }
    }
}