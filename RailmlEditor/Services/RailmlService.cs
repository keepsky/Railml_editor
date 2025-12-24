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
                    Tracks = new Tracks()
                }
            };
            railml.Namespaces.Add("sehwa", "http://www.sehwa.co.kr/railml");

            // 1. Create Tracks
            var trackMap = new System.Collections.Generic.Dictionary<string, Track>();
            foreach (var element in viewModel.Elements.OfType<TrackViewModel>())
            {
                var track = new Track
                {
                    Id = element.Id,
                    Name = element.Name,
                    Description = element.Description,
                    Type = element.Type,
                    MainDir = element.MainDir,
                    TrackTopology = new TrackTopology
                    {
                        TrackBegin = new TrackNode 
                        { 
                            Id = $"{element.Id}_begin",
                            Pos = 0,
                            X = element.X, 
                            Y = element.Y,
                            ScreenPos = element is CurvedTrackViewModel ctv 
                                ? new ScreenPos { MX = ctv.MX, MY = ctv.MY } 
                                : null
                        },
                        TrackEnd = new TrackNode 
                        { 
                            Id = $"{element.Id}_end", 
                            Pos = (int)element.Length,
                            X = element.X2, 
                            Y = element.Y2 
                        },
                    },
                     // OcsElements initialized conditionally below
                };

                // Add Signals bound to this track
                var boundSignals = viewModel.Elements.OfType<SignalViewModel>().Where(s => s.RelatedTrackId == element.Id).ToList();
                
                if (boundSignals.Count > 0)
                {
                    track.OcsElements = new OcsElements { Signals = new Signals() };
                    
                    foreach (var sigVm in boundSignals)
                    {
                        // Calculate Pos relative to track start
                        double dist = Math.Sqrt(Math.Pow(sigVm.X - element.X, 2) + Math.Pow(sigVm.Y - element.Y, 2));
                        
                        var signal = new Signal
                        {
                            Id = sigVm.Id,
                            Dir = sigVm.Direction,
                            Type = sigVm.Type,
                            Function = sigVm.Function,
                            AdditionalName = new AdditionalName { Name = sigVm.Name },
                            Pos = (int)dist, 
                            X = sigVm.X,
                            Y = sigVm.Y
                        };
                        track.OcsElements.Signals.SignalList.Add(signal);
                    }
                }

                railml.Infrastructure.Tracks.TrackList.Add(track);
                trackMap[element.Id] = track;
            }

            // 2. Add Signals & TCBs (REMOVED global signals additions)
            // Just kept loop structure for clarity or removal
            // Signals are now handled inside Track loop.

            // 3. Auto-Generate Connections & Switches
            var allNodes = new System.Collections.Generic.List<TrackNode>();
            foreach(var track in railml.Infrastructure.Tracks.TrackList)
            {
                allNodes.Add(track.TrackTopology.TrackBegin);
                allNodes.Add(track.TrackTopology.TrackEnd);
            }

            foreach(var nodeA in allNodes)
            {
                var overlappingNodes = new System.Collections.Generic.List<TrackNode>();
                foreach(var nodeB in allNodes)
                {
                    if(nodeA == nodeB) continue;
                    bool isBeginA = nodeA.Id.EndsWith("_begin");
                    bool isBeginB = nodeB.Id.EndsWith("_begin");
                    if (isBeginA == isBeginB) continue; 

                    double dist = Math.Sqrt(Math.Pow(nodeA.X - nodeB.X, 2) + Math.Pow(nodeA.Y - nodeB.Y, 2));
                    if(dist < 1.0) overlappingNodes.Add(nodeB);
                }

                if (overlappingNodes.Count > 0)
                {
                    if (nodeA.Connections == null) nodeA.Connections = new NodeConnections();
                    
                    bool isTrackEnd = nodeA.Id.EndsWith("_end");

                    if (!isTrackEnd || overlappingNodes.Count == 1)
                    {
                         foreach (var nodeB in overlappingNodes)
                         {
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
                    else
                    {
                         if (nodeA.Connections.Switches.Count == 0)
                         {
                             var firstNode = overlappingNodes[0];
                             
                             // Attempt to find a SwitchViewModel overlapping this node to get its Name
                             var switchVm = viewModel.Elements.OfType<SwitchViewModel>()
                                 .FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - nodeA.X, 2) + Math.Pow(s.Y - nodeA.Y, 2)) < 5.0);

                             var switchObj = new Switch
                             {
                                 Id = switchVm?.Id ?? $"sw_{nodeA.Id}",
                                 AdditionalName = new AdditionalName { Name = switchVm?.Name },
                                 Ref = firstNode.Id
                             };
                             for (int i = 1; i < overlappingNodes.Count; i++)
                             {
                                 switchObj.ConnectionList.Add(new Connection
                                 {
                                     Id = $"switch_conn_{switchObj.Id}_{i}",
                                     Ref = overlappingNodes[i].Id
                                 });
                             }
                             nodeA.Connections.Switches.Add(switchObj);
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
                        TrackViewModel trackVm;
                        
                        if (track.TrackTopology?.TrackBegin?.ScreenPos != null)
                        {
                            var ctv = new CurvedTrackViewModel
                            {
                                MX = track.TrackTopology.TrackBegin.ScreenPos.MX,
                                MY = track.TrackTopology.TrackBegin.ScreenPos.MY
                            };
                            trackVm = ctv;
                        }
                        else
                        {
                            trackVm = new TrackViewModel();
                        }

                        trackVm.Id = track.Id;
                        trackVm.Name = track.Name;
                        trackVm.Description = track.Description;
                        trackVm.Type = track.Type;
                        trackVm.MainDir = track.MainDir;
                        trackVm.X = track.TrackTopology?.TrackBegin?.X ?? 0;
                        trackVm.Y = track.TrackTopology?.TrackBegin?.Y ?? 0;
                        
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
                        
                        // Load Switches from TrackBegin
                        if (track.TrackTopology?.TrackBegin?.Connections?.Switches != null)
                        {
                            foreach (var sw in track.TrackTopology.TrackBegin.Connections.Switches)
                            {
                                // Check if already added (Switches might be referenced by multiple tracks? No, defined in one node)
                                if (!viewModel.Elements.Any(e => e.Id == sw.Id))
                                {
                                    var switchVm = new SwitchViewModel
                                    {
                                        Id = sw.Id,
                                        Name = sw.AdditionalName?.Name,
                                        X = trackVm.X, // Switch is at Node position
                                        Y = trackVm.Y
                                    };
                                    viewModel.Elements.Add(switchVm);
                                }
                            }
                        }
                        // Load Switches from TrackEnd
                        if (track.TrackTopology?.TrackEnd?.Connections?.Switches != null)
                        {
                            foreach (var sw in track.TrackTopology.TrackEnd.Connections.Switches)
                            {
                                if (!viewModel.Elements.Any(e => e.Id == sw.Id))
                                {
                                    var switchVm = new SwitchViewModel
                                    {
                                        Id = sw.Id,
                                        Name = sw.AdditionalName?.Name,
                                        X = track.TrackTopology.TrackEnd.X,
                                        Y = track.TrackTopology.TrackEnd.Y
                                    };
                                    viewModel.Elements.Add(switchVm);
                                }
                            }
                        }

                        // Load Signals
                        if (track.OcsElements?.Signals?.SignalList != null)
                        {
                            foreach (var signal in track.OcsElements.Signals.SignalList)
                            {
                                var signalVm = new SignalViewModel
                                {
                                    Id = signal.Id,
                                    Direction = signal.Dir ?? "up",
                                    Type = signal.Type,
                                    Function = signal.Function,
                                    Name = signal.AdditionalName?.Name,
                                    X = signal.X,
                                    Y = signal.Y,
                                    RelatedTrackId = track.Id
                                };

                                // Infer Flipped state from geometry if Dir missing or consistent check
                                // Priority: geometry if loaded? 
                                // Actually, if we have Dir, trust Dir. But ensure geometry matches?
                                // User: "Calculate IsFlipped based on Y position" was previous logic.
                                // Now we have Explicit Dir.
                                // If Dir is set, we use it. 
                                // But if Dir is "up" but Y is below, should we fix Dir or fix Y?
                                // Let's trust Dir primarily, but if Dir is missing/null, use geometry.
                                if (string.IsNullOrEmpty(signal.Dir))
                                { 
                                     if (signal.Y > trackVm.Y + 5)
                                     {
                                         signalVm.Direction = "down";
                                     }
                                }
                                
                                // Fallback if X/Y missing in XML (rely on Pos=0/Track)
                                if (signal.X == 0 && signal.Y == 0 && signal.Pos == 0)
                                {
                                     signalVm.X = trackVm.X;
                                     signalVm.Y = trackVm.Y;
                                     // Apply offset based on Dir
                                     signalVm.Y += (signalVm.Direction == "down" ? 20 : -20);
                                     // Actually trackVm.Y is center line? Visuals currently assume track is line.
                                     // Snapping logic puts it at +/- 20.
                                     // So we should respect that.
                                }

                                viewModel.Elements.Add(signalVm);
                            }
                        }
                    }
                }

                // Removed global Signal loading loop
            }
        }
    }
}
