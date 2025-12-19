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
        
        // Visual Coordinates (Extension, not part of strict RailML but needed for Editor)
        [XmlAttribute]
        public double X { get; set; }
        [XmlAttribute]
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

        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }
    }
}
