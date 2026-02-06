using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TestNamespace
{
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

        [XmlElement(ElementName = "trackTopology")]
        public TrackTopology TrackTopology { get; set; }
    }

    public class TrackTopology
    {
        [XmlElement(ElementName = "trackBegin")]
        public TrackNode TrackBegin { get; set; }

        [XmlElement(ElementName = "trackEnd")]
        public TrackNode TrackEnd { get; set; }
    }

    public class TrackNode
    {
        [XmlAttribute(AttributeName = "pos")]
        public double Pos { get; set; }

        [XmlAttribute(AttributeName = "absPos")]
        public double AbsPos { get; set; }

        [XmlAttribute(AttributeName = "id")]
        public string Id { get; set; }
    }

    public class Program
    {
        public static void Main()
        {
            string path = @"tt.railml";

            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine("File not found: " + Path.GetFullPath(path));
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(Railml));
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    var railml = (Railml)serializer.Deserialize(fs);
                    if (railml?.Infrastructure?.Tracks?.TrackList != null)
                    {
                        foreach(var track in railml.Infrastructure.Tracks.TrackList)
                        {
                            if (track.TrackTopology?.TrackBegin != null)
                                Console.WriteLine($"Track {track.Id} Begin AbsPos: {track.TrackTopology.TrackBegin.AbsPos}");
                            if (track.TrackTopology?.TrackEnd != null)
                                Console.WriteLine($"Track {track.Id} End AbsPos: {track.TrackTopology.TrackEnd.AbsPos}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to deserialize tracks.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.ToString());
            }
        }
    }
}
