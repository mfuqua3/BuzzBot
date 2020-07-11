using System.Xml.Serialization;

namespace BuzzBot.Wowhead
{
    [XmlRoot(ElementName = "subclass")]
    public class Subclass
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        [XmlText]
        public string Text { get; set; }
    }
}