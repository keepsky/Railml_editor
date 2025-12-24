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
                // Determine if Curved
                bool isCurved = element is CurvedTrackViewModel;
                CurvedTrackViewModel ctv = isCurved ? (CurvedTrackViewModel)element : null;

                var track = new Track
                {
                    Id = GetRailmlId(element.Id),
                    Name = element.Name,
                    Description = element.Description,
                    Type = element.Type,
                    MainDir = element.MainDir,
                    Code = isCurved ? "corner" : "plain",
                    TrackTopology = new TrackTopology
                    {
                        TrackBegin = new TrackNode 
                        { 
                            Id = $"{GetRailmlId(element.Id)}_begin",
                            Pos = 0,
                            ScreenPos = new ScreenPos
                            {
                                X = element.X,
                                XSpecified = true,
                                Y = element.Y,
                                YSpecified = true
                            }
                        },
                        TrackEnd = new TrackNode 
                        { 
                            Id = $"{GetRailmlId(element.Id)}_end", 
                            Pos = (int)element.Length,
                            ScreenPos = new ScreenPos
                            {
                                X = element.X2,
                                XSpecified = true,
                                Y = element.Y2,
                                YSpecified = true
                            }
                        },
                        CornerPos = isCurved ? new CornerPos { X = ctv.MX, Y = ctv.MY } : null
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
            // 3. Auto-Generate Connections & Switches
            var allNodes = new System.Collections.Generic.List<TrackNode>();
            var nodeToTrack = new System.Collections.Generic.Dictionary<TrackNode, Track>(); // Map Node -> Track

            foreach(var track in railml.Infrastructure.Tracks.TrackList)
            {
                allNodes.Add(track.TrackTopology.TrackBegin);
                nodeToTrack[track.TrackTopology.TrackBegin] = track;

                allNodes.Add(track.TrackTopology.TrackEnd);
                nodeToTrack[track.TrackTopology.TrackEnd] = track;
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

                    double ax = nodeA.ScreenPos?.X ?? 0;
                    double ay = nodeA.ScreenPos?.Y ?? 0;
                    double bx = nodeB.ScreenPos?.X ?? 0;
                    double by = nodeB.ScreenPos?.Y ?? 0;

                    double dist = Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
                    if(dist < 1.0) overlappingNodes.Add(nodeB);
                }

                if (overlappingNodes.Count > 0)
                {
                    // Identify Parent Track
                    var parentTrack = nodeToTrack[nodeA];
                    if (parentTrack.TrackTopology.Connections == null)
                        parentTrack.TrackTopology.Connections = new Connections();

                    // Logic Change: Add to TrackTopology.Connections instead of nodeA.Connections
                    
                    // bool isTrackEnd = nodeA.Id.EndsWith("_end");
                    
                    if (overlappingNodes.Count == 1)
                    {
                         foreach (var nodeB in overlappingNodes)
                         {
                             // Simple Connection
                             if(!nodeA.ConnectionList.Any(c => c.Ref == nodeB.Id))
                             {
                                 nodeA.ConnectionList.Add(new Connection 
                                 { 
                                     Id = $"conn_{nodeA.Id}_to_{nodeB.Id}",
                                     Ref = nodeB.Id 
                                 });
                             }
                         }
                    }
                    else
                    {
                         // Switch Connection
                         // Check if switch already exists (by Ref?) - basic check
                         // Assuming strictly one switch per node for simplified logic
                         // Ensure Connections object exists
                         if (parentTrack.TrackTopology.Connections == null) 
                             parentTrack.TrackTopology.Connections = new Connections();

                         // Check if switch already added for this node
                         string expectedSwitchId = $"sw_{nodeA.Id}";
                         // Also check if we should use existing ID from ViewModel?
                         var switchVmForCheck = viewModel.Elements.OfType<SwitchViewModel>()
                                 .FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - (nodeA.ScreenPos?.X ?? 0), 2) + Math.Pow(s.Y - (nodeA.ScreenPos?.Y ?? 0), 2)) < 5.0);
                         if (switchVmForCheck != null) expectedSwitchId = switchVmForCheck.Id;

                         if (!parentTrack.TrackTopology.Connections.Switches.Any(s => s.Id == expectedSwitchId))
                         {
                             // Identify Switch VM
                             double switchPos = nodeA.Pos; 

                             var switchObj = new Switch
                             {
                                 Id = expectedSwitchId,
                                 AdditionalName = new AdditionalName { Name = switchVmForCheck?.Name },
                                 Pos = switchPos,
                                 ScreenPos = (switchVmForCheck?.MX.HasValue == true && switchVmForCheck?.MY.HasValue == true)
                                     ? new ScreenPos { X = switchVmForCheck.MX.Value, XSpecified = true, Y = switchVmForCheck.MY.Value, YSpecified = true }
                                     : null
                             };

                             foreach (var nodeB in overlappingNodes)
                             {
                                 switchObj.ConnectionList.Add(new Connection
                                 {
                                     Id = $"conn_{nodeA.Id}_to_{nodeB.Id}",
                                     Ref = nodeB.Id
                                 });
                             }
                             parentTrack.TrackTopology.Connections.Switches.Add(switchObj);
                         }
                    }
                }
            }


            // Serialize
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Railml));
                using (TextWriter writer = new StreamWriter(path))
                {
                    serializer.Serialize(writer, railml);
                }
            }
            catch (Exception ex)
            {
                // Simple logging to debug
                File.WriteAllText("save_error.txt", ex.ToString());
                throw; // Re-throw to let UI handle or crash
            }
        }


        public void Load(string path, MainViewModel viewModel)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                var railml = (Railml?)serializer.Deserialize(fs);

                viewModel.Elements.Clear();

                if (railml?.Infrastructure?.Tracks?.TrackList != null)
                {
                    foreach (var track in railml.Infrastructure.Tracks.TrackList)
                    {
                        TrackViewModel trackVm;
                        
                        if (track.Code == "corner" && track.TrackTopology?.CornerPos != null)
                        {
                            var ctv = new CurvedTrackViewModel
                            {
                                MX = track.TrackTopology.CornerPos.X,
                                MY = track.TrackTopology.CornerPos.Y
                            };
                            trackVm = ctv;
                        }
                        else if (track.TrackTopology?.TrackBegin?.ScreenPos != null && track.TrackTopology.TrackBegin.ScreenPos.MXSpecified)
                        {
                            // Backward Compatibility: Load from ScreenPos
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
                        trackVm.X = track.TrackTopology?.TrackBegin?.ScreenPos?.X ?? 0;
                        trackVm.Y = track.TrackTopology?.TrackBegin?.ScreenPos?.Y ?? 0;
                        
                        // Set X2, Y2 from TrackEnd
                        if (track.TrackTopology?.TrackEnd != null)
                        {
                            trackVm.X2 = track.TrackTopology.TrackEnd.ScreenPos?.X ?? 0;
                            trackVm.Y2 = track.TrackTopology.TrackEnd.ScreenPos?.Y ?? 0;
                        }
                        else
                        {
                            // Default length 100 if no end specified
                            trackVm.Length = 100; 
                        }

                        viewModel.Elements.Add(trackVm);
                        

                        // NEW: Load Switches from TrackTopology.Connections (Standardized Location)
                        if (track.TrackTopology?.Connections?.Switches != null)
                        {
                            foreach (var sw in track.TrackTopology.Connections.Switches)
                            {
                                if (!viewModel.Elements.Any(e => e.Id == sw.Id))
                                {
                                    // Calculate Position based on 'pos' attribute
                                    double pos = sw.Pos;
                                    double startX = trackVm.X;
                                    double startY = trackVm.Y;
                                    double endX = trackVm.X2;
                                    double endY = trackVm.Y2;
                                    
                                    // Use TrackViewModel Length if available, or calculate distance
                                    double length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                                    if (length < 0.1) length = 1; // Avoid divide by zero

                                    // Linear Interpolation for X/Y
                                    double ratio = pos / length;
                                    double swX = startX + ratio * (endX - startX);
                                    double swY = startY + ratio * (endY - startY);

                                    var switchVm = new SwitchViewModel
                                    {
                                        Id = sw.Id,
                                        Name = sw.AdditionalName?.Name,
                                        X = swX,
                                        Y = swY,
                                        MX = (sw.ScreenPos != null && sw.ScreenPos.XSpecified) ? sw.ScreenPos.X : (double?)null,
                                        MY = (sw.ScreenPos != null && sw.ScreenPos.YSpecified) ? sw.ScreenPos.Y : (double?)null
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
        private string GetRailmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            if (id.StartsWith("PT")) return id.Replace("PT", "T");
            return id;
        }
    }
}
