using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace DefaultNamespace
{
    // using System.Xml.Serialization;
// XmlSerializer serializer = new XmlSerializer(typeof(SmoothWalk));
// using (StringReader reader = new StringReader(xml))
// {
//    var test = (SmoothWalk)serializer.Deserialize(reader);
// }

    [XmlRoot(ElementName = "monitor")]
    public class Monitor
    {
        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

        [XmlAttribute(AttributeName = "ip")] public string Ip { get; set; }
    }

    [XmlRoot(ElementName = "log-binary")]
    public class Logbinary
    {
        [XmlAttribute(AttributeName = "pin")] public int Pin { get; set; }
    }

    [XmlRoot(ElementName = "sprite")]
    public class Sprite
    {
        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

        [XmlAttribute(AttributeName = "color")]
        public string Color { get; set; }

        [XmlAttribute(AttributeName = "extents")]
        public string Extents { get; set; }

        [XmlElement(ElementName = "trigger")] public Trigger Trigger { get; set; }
    }

    [XmlRoot(ElementName = "call")]
    public class Call
    {
        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }
    }

    [XmlRoot(ElementName = "timer")]
    public class Timer
    {
        [XmlElement(ElementName = "call")] public Call Call { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public int Duration { get; set; }

        [XmlElement(ElementName = "sprite")] public Sprite Sprite { get; set; }

        [XmlElement(ElementName="timer")] 
        public EncapsulatedTimer EncapsulatedTimer { get; set; } 
    }

    [XmlRoot(ElementName = "timer")]
    public class EncapsulatedTimer
    {
        [XmlElement(ElementName = "call")] public Call Call { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public int Duration { get; set; }

        [XmlElement(ElementName = "sprite")] public Sprite Sprite { get; set; }
    }

    [XmlRoot(ElementName = "load")]
    public class Load
    {
        [XmlAttribute(AttributeName = "bundle")]
        public string Bundle { get; set; }
    }

    [XmlRoot(ElementName = "function")]
    public class Function
    {
        [XmlElement(ElementName = "log-binary", IsNullable = true)] public Logbinary Logbinary { get; set; }
        
        [XmlElement(ElementName = "sprite")] public List<Sprite> Sprite { get; set; }

        [XmlElement(ElementName = "load")] public List<Load> Load { get; set; }

        [XmlElement(ElementName = "object")] public List<Object> Object { get; set; }

        [XmlElement(ElementName = "tone")] public Tone Tone { get; set; }

        [XmlElement(ElementName = "note")] public Note Note { get; set; }

        [XmlElement(ElementName = "pulse")] public List<Pulse> Pulse { get; set; }

        [XmlElement(ElementName = "group")] public Group Group { get; set; }

        [XmlElement(ElementName = "timer")] public Timer Timer { get; set; }

        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

        [XmlElement(ElementName = "collection")]
        public List<Collection> Collection { get; set; }
    }

    [XmlRoot(ElementName = "collection")]
    public class Collection
    {
        [XmlElement(ElementName = "call")] public List<Call> Call { get; set; }

        [XmlAttribute(AttributeName = "sample")]
        public string Sample { get; set; }

        [XmlElement(ElementName = "object")] public List<Object> Object { get; set; }

        [XmlElement(ElementName = "group")] public List<Group> Group { get; set; }
    }

    [XmlRoot(ElementName = "object")]
    public class Object
    {
        [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

        [XmlAttribute(AttributeName = "bundle")]
        public string Bundle { get; set; }

        [XmlAttribute(AttributeName = "extents")]
        public string Extents { get; set; }

        [XmlAttribute(AttributeName = "si")] public int Si { get; set; }

        [XmlAttribute(AttributeName = "sj")] public int Sj { get; set; }

        [XmlAttribute(AttributeName = "sk")] public int Sk { get; set; }

        [XmlAttribute(AttributeName = "j")] public int J { get; set; }
    }

    [XmlRoot(ElementName = "tone")]
    public class Tone
    {
        [XmlAttribute(AttributeName = "frequency")]
        public int Frequency { get; set; }

        [XmlAttribute(AttributeName = "duration")]
        public double Duration { get; set; }
    }

    [XmlRoot(ElementName = "note")]
    public class Note
    {
        [XmlAttribute(AttributeName = "text")] public string Text { get; set; }
    }

    [XmlRoot(ElementName = "pulse")]
    public class Pulse
    {
        [XmlAttribute(AttributeName = "pin")] public int Pin { get; set; }

        [XmlAttribute(AttributeName = "duration-high")]
        public double DurationHigh { get; set; }
    }

    [XmlRoot(ElementName = "group")]
    public class Group
    {
        [XmlElement(ElementName = "pulse")] public List<Pulse> Pulse { get; set; }

        [XmlAttribute(AttributeName = "target")]
        public string Target { get; set; }

        [XmlElement(ElementName = "sprite")] public List<Sprite> Sprite { get; set; }

        [XmlElement(ElementName = "object")] public List<Object> Object { get; set; }
    }

    [XmlRoot(ElementName = "begin")]
    public class Begin
    {
        [XmlElement(ElementName = "call")] public Call Call { get; set; }
    }

    [XmlRoot(ElementName = "trigger")]
    public class Trigger
    {
        [XmlElement(ElementName = "begin")] public Begin Begin { get; set; }
    }

    [XmlRoot(ElementName = "SmoothWalk")]
    public class SmoothWalk
    {
        [XmlElement(ElementName = "broadcast")]
        public object Broadcast { get; set; }

        [XmlElement(ElementName = "monitor")] public Monitor Monitor { get; set; }

        [XmlElement(ElementName = "function")] public List<Function> Function { get; set; }
    }
}