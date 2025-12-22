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

        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }

        // Visual Coordinates
        [XmlAttribute]
        public double X { get; set; }
        [XmlAttribute]
        public double Y { get; set; }
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

        [XmlElement(ElementName = "trackTopology")]
        public TrackTopology TrackTopology { get; set; }
    }

    public class TrackTopology
    {
        [XmlElement(ElementName = "trackBegin")]
        public TrackNode TrackBegin { get; set; }

        [XmlElement(ElementName = "trackEnd")]
        public TrackNode TrackEnd { get; set; }

        [XmlElement(ElementName = "connections")]
        public Connections Connections { get; set; }
    }

    public class TrackNode
    {
        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
        
        [XmlElement("any")]
        public SehwaAny Any { get; set; }

        [XmlElement(ElementName = "connections")]
        public NodeConnections Connections { get; set; }

        // Helper properties for Editor logic (mapped to Any)
        [XmlIgnore]
        public double X 
        { 
            get => Any?.ScreenPos?.X ?? 0;
            set 
            {
                if (Any == null) Any = new SehwaAny();
                if (Any.ScreenPos == null) Any.ScreenPos = new SehwaScreenPos();
                Any.ScreenPos.X = value;
            }
        }

        [XmlIgnore]
        public double Y
        {
            get => Any?.ScreenPos?.Y ?? 0;
            set
            {
                if (Any == null) Any = new SehwaAny();
                if (Any.ScreenPos == null) Any.ScreenPos = new SehwaScreenPos();
                Any.ScreenPos.Y = value;
            }
        }
    }

    public class NodeConnections
    {
        [XmlElement(ElementName = "switch")]
        public List<Switch> Switches { get; set; } = new List<Switch>();

        [XmlElement(ElementName = "connection")]
        public List<Connection> ConnectionList { get; set; } = new List<Connection>();
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

    public class SehwaAny
    {
        [XmlElement("screenPos", Namespace = "http://www.sehwa.co.kr/railml")]
        public SehwaScreenPos ScreenPos { get; set; }
    }

    public class SehwaScreenPos
    {
        [XmlAttribute("x")]
        public double X { get; set; }

        [XmlAttribute("y")]
        public double Y { get; set; }
    }

    public class Connections
    {
        [XmlElement(ElementName = "switch")]
        public List<Switch> Switches { get; set; } = new List<Switch>();
    }

    public class Switch
    {
        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }

        [XmlAttribute(AttributeName = "ref")]
        public string Ref { get; set; }

        [XmlElement(ElementName = "connection")]
        public List<Connection> ConnectionList { get; set; } = new List<Connection>();
    }
}
