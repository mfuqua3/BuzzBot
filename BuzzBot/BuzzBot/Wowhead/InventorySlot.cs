using System.Xml.Serialization;

namespace BuzzBot.Wowhead
{
    [XmlRoot(ElementName = "inventorySlot")]
    public class InventorySlot
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlText]
        public string Text { get; set; }
    }
}