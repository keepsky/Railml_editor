using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Services
{
    public class RailmlService
    {
        public void Save(string path, MainViewModel viewModel)
        {
            var railml = new Railml
            {
                Infrastructure = new Infrastructure
                {
                    Tracks = new Tracks(),
                    Signals = new Signals()
                }
            };
            railml.Namespaces.Add("sehwa", "http://www.sehwa.co.kr/railml");


            // Map ViewModels to Data Models
            foreach (var element in viewModel.Elements)
            {
                if (element is TrackViewModel trackVm)
                {
                    var track = new Track
                    {
                        Id = trackVm.Id,
                        Name = "Generated Track",
                        TrackTopology = new TrackTopology
                        {
                            TrackBegin = new TrackNode 
                            { 
                                Id = $"{trackVm.Id}_begin",
                                Pos = 0,
                                X = trackVm.X, 
                                Y = trackVm.Y 
                            },
                            TrackEnd = new TrackNode 
                            { 
                                Id = $"{trackVm.Id}_end", 
                                Pos = trackVm.Length, // Should be actual length
                                X = trackVm.X2, 
                                Y = trackVm.Y2 
                            },
                             Connections = new Connections() 
                        }
                    };
                    railml.Infrastructure.Tracks.TrackList.Add(track);
                }
                else if (element is SwitchViewModel switchVm)
                {
                     // ... switch logic ...
                }
                else if (element is SignalViewModel signalVm)
                {
                    var signal = new Signal
                    {
                        Id = signalVm.Id,
                        X = signalVm.X,
                        Y = signalVm.Y
                    };
                    railml.Infrastructure.Signals.SignalList.Add(signal);
                }
            }

            // Auto-Generate Connections based on Overlap
            var allNodes = new System.Collections.Generic.List<TrackNode>();
            
            // Collect all nodes
            foreach(var track in railml.Infrastructure.Tracks.TrackList)
            {
                if(track.TrackTopology?.TrackBegin != null) allNodes.Add(track.TrackTopology.TrackBegin);
                if(track.TrackTopology?.TrackEnd != null) allNodes.Add(track.TrackTopology.TrackEnd);
            }

            // Check for overlaps (Distance < 1.0 for tolerance)
            foreach(var nodeA in allNodes)
            {
                foreach(var nodeB in allNodes)
                {
                    if(nodeA == nodeB) continue;

                    double dist = Math.Sqrt(Math.Pow(nodeA.X - nodeB.X, 2) + Math.Pow(nodeA.Y - nodeB.Y, 2));
                    if(dist < 1.0)
                    {
                        // Overlap detected!
                        if(nodeA.Connections == null) nodeA.Connections = new NodeConnections();
                        
                        // Check if connection already exists
                        if(!nodeA.Connections.ConnectionList.Any(c => c.Ref == nodeB.Id))
                        {
                            nodeA.Connections.ConnectionList.Add(new Connection 
                            { 
                                Id = $"conn_{nodeA.Id}_to_{nodeB.Id}",
                                Ref = nodeB.Id 
                            });
                        }
                    }
                }
            }


            // Serialize
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (TextWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, railml);
            }
        }

        public void Load(string path, MainViewModel viewModel)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                var railml = (Railml)serializer.Deserialize(fs);

                viewModel.Elements.Clear();

                if (railml.Infrastructure?.Tracks?.TrackList != null)
                {
                    foreach (var track in railml.Infrastructure.Tracks.TrackList)
                    {
                        var trackVm = new TrackViewModel
                        {
                            Id = track.Id,
                            X = track.TrackTopology?.TrackBegin?.X ?? 0,
                            Y = track.TrackTopology?.TrackBegin?.Y ?? 0,
                        };
                        
                        // Set X2, Y2 from TrackEnd
                        if (track.TrackTopology?.TrackEnd != null)
                        {
                            trackVm.X2 = track.TrackTopology.TrackEnd.X;
                            trackVm.Y2 = track.TrackTopology.TrackEnd.Y;
                        }
                        else
                        {
                            // Default length 100 if no end specified
                            trackVm.Length = 100; 
                        }

                        viewModel.Elements.Add(trackVm);
                    }
                }

                if (railml.Infrastructure?.Signals?.SignalList != null)
                {
                     foreach (var signal in railml.Infrastructure.Signals.SignalList)
                     {
                         var signalVm = new SignalViewModel
                         {
                             Id = signal.Id,
                             X = signal.X,
                             Y = signal.Y
                         };
                         viewModel.Elements.Add(signalVm);
                     }
                }
            }
        }
    }
}
