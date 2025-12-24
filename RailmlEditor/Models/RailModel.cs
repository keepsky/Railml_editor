using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace RailmlEditor.Models
{
    // Simplified RailML 2.5 Structure

    [XmlRoot(ElementName = "railml", Namespace = "http://www.railml.org/schemas/2013")]
    public class Railml
    {
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; } = "2.5";

        [XmlElement(ElementName = "infrastructure")]
        public Infrastructure Infrastructure { get; set; }

        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces Namespaces { get; set; } = new XmlSerializerNamespaces();
    }


    public class Infrastructure
    {
        [XmlElement(ElementName = "tracks")]
        public Tracks Tracks { get; set; }
    }

    public class Tracks
    {
        [XmlElement(ElementName = "track")]
        public List<Track> TrackList { get; set; } = new List<Track>();
    }

    public class Track
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "description")]
        public string Description { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "mainDir")]
        public string MainDir { get; set; }

        [XmlElement(ElementName = "trackTopology")]
        public TrackTopology TrackTopology { get; set; }

        [XmlElement(ElementName = "ocsElements")]
        public OcsElements OcsElements { get; set; }

        [XmlAttribute(AttributeName = "code")]
        public string Code { get; set; }
    }

    public class OcsElements
    {
        [XmlElement(ElementName = "signals")]
        public Signals Signals { get; set; }
    }

    public class Signals
    {
        [XmlElement(ElementName = "signal")]
        public List<Signal> SignalList { get; set; } = new List<Signal>();
    }

    public class Signal
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "dir")]
        public string Dir { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "function")]
        public string Function { get; set; }

        // Mapped to <additionalName name="...">
        [XmlElement(ElementName = "additionalName")]
        public AdditionalName AdditionalName { get; set; }

        [XmlAttribute(AttributeName = "pos")]
        public int Pos { get; set; }

        // Visual Coordinates (Legacy attributes, hide from Save but keep for Load)
        [XmlAttribute]
        public double X { get; set; }
        public bool ShouldSerializeX() => false;

        [XmlAttribute]
        public double Y { get; set; }
        public bool ShouldSerializeY() => false;

        [XmlElement(ElementName = "screenPos", Namespace = "http://www.sehwa.co.kr/railml")]
        public ScreenPos ScreenPos { get; set; }
    }

    public class AdditionalName
    {
        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }
    }

    public class TrackTopology
    {
        [XmlElement(ElementName = "trackBegin")]
        public TrackNode TrackBegin { get; set; }

        [XmlElement(ElementName = "trackEnd")]
        public TrackNode TrackEnd { get; set; }

        [XmlElement(ElementName = "connections")]
        public Connections Connections { get; set; }

        [XmlElement(ElementName = "cornerPos", Namespace = "http://www.sehwa.co.kr/railml")]
        public CornerPos CornerPos { get; set; }
    }

    public class CornerPos
    {
        [XmlAttribute(AttributeName = "x")]
        public double X { get; set; }

        [XmlAttribute(AttributeName = "y")]
        public double Y { get; set; }
    }

    public class TrackNode
    {
        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        
        [XmlElement(ElementName = "connection")]
        public List<Connection> ConnectionList { get; set; } = new List<Connection>();

        [XmlElement(ElementName = "screenPos", Namespace = "http://www.sehwa.co.kr/railml")]
        public ScreenPos ScreenPos { get; set; }
    }

    public class Connection
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "ref")]
        public string Ref { get; set; }

        [XmlAttribute(AttributeName = "orientation")]
        public string Orientation { get; set; }

        [XmlAttribute(AttributeName = "course")]
        public string Course { get; set; }
    }

    public class Connections
    {
        [XmlElement(ElementName = "switch")]
        public List<Switch> Switches { get; set; } = new List<Switch>();

        [XmlElement(ElementName = "connection")]
        public List<Connection> ConnectionList { get; set; } = new List<Connection>();
    }

    public class Switch
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }

        [XmlElement(ElementName = "screenPos", Namespace = "http://www.sehwa.co.kr/railml")]
        public ScreenPos ScreenPos { get; set; }

        [XmlElement(ElementName = "additionalName")]
        public AdditionalName AdditionalName { get; set; }

        [XmlElement(ElementName = "connection")]
        public List<Connection> ConnectionList { get; set; } = new List<Connection>();
    }


    public class ScreenPos
    {
        [XmlAttribute(AttributeName = "mx")]
        public double MX { get; set; }

        [XmlIgnore]
        public bool MXSpecified { get; set; }

        [XmlAttribute(AttributeName = "my")]
        public double MY { get; set; }

        [XmlIgnore]
        public bool MYSpecified { get; set; }

        [XmlAttribute(AttributeName = "x")]
        public double X { get; set; }

        [XmlIgnore]
        public bool XSpecified { get; set; }

        [XmlAttribute(AttributeName = "y")]
        public double Y { get; set; }

        [XmlIgnore]
        public bool YSpecified { get; set; }
    }
}

