using System.Xml.Serialization;

namespace BuzzBot.Wowhead
{
    [XmlRoot(ElementName = "item")]
    public class XmlWarcraftItem
    {
        [XmlElement(ElementName = "name")]
        public string Name { get; set; }
        [XmlElement(ElementName = "level")]
        public string Level { get; set; }
        [XmlElement(ElementName = "quality")]
        public Quality Quality { get; set; }
        [XmlElement(ElementName = "class")]
        public Class Class { get; set; }
        [XmlElement(ElementName = "subclass")]
        public Subclass Subclass { get; set; }
        [XmlElement(ElementName = "icon")]
        public Icon Icon { get; set; }
        [XmlElement(ElementName = "inventorySlot")]
        public InventorySlot InventorySlot { get; set; }
        [XmlElement(ElementName = "htmlTooltip")]
        public string HtmlTooltip { get; set; }
        [XmlElement(ElementName = "json")]
        public string Json { get; set; }
        [XmlElement(ElementName = "jsonEquip")]
        public string JsonEquip { get; set; }
        [XmlElement(ElementName = "createdBy")]
        public CreatedBy CreatedBy { get; set; }
        [XmlElement(ElementName = "link")]
        public string Link { get; set; }
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }
}