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
                    Signals = new Signals(),
                    TvdSections = new TvdSections()
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
                    Name = "Generated Track",
                    TrackTopology = new TrackTopology
                    {
                        TrackBegin = new TrackNode 
                        { 
                            Id = $"{element.Id}_begin",
                            Pos = 0,
                            X = element.X, 
                            Y = element.Y 
                        },
                        TrackEnd = new TrackNode 
                        { 
                            Id = $"{element.Id}_end", 
                            Pos = element.Length,
                            X = element.X2, 
                            Y = element.Y2 
                        },
                         Connections = new Connections() 
                    },
                    TrainDetectionElements = new TrainDetectionElements()
                };
                railml.Infrastructure.Tracks.TrackList.Add(track);
                trackMap[element.Id] = track;
            }

            // 2. Add Signals & TCBs
            foreach (var element in viewModel.Elements)
            {
                if (element is SignalViewModel signalVm)
                {
                    var signal = new Signal { Id = signalVm.Id, X = signalVm.X, Y = signalVm.Y };
                    railml.Infrastructure.Signals.SignalList.Add(signal);
                }
                else if (element is TcbViewModel tcbVm)
                {
                    if (trackMap.ContainsKey(tcbVm.ParentTrackId))
                    {
                        var track = trackMap[tcbVm.ParentTrackId];
                        track.TrainDetectionElements.TrackCircuitBorders.Add(new TrackCircuitBorder
                        {
                            Id = tcbVm.Id,
                            Pos = tcbVm.PositionOnTrack,
                            Dir = tcbVm.Dir
                        });
                    }
                }
            }

            // 3. Auto-Generate Connections
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
                             var switchObj = new Switch
                             {
                                 Id = $"sw_{nodeA.Id}",
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

            // 4. Auto-Generate TVD Sections
            GenerateTvdSections(railml);

            // Serialize
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (TextWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, railml);
            }
        }

        private void GenerateTvdSections(Railml railml)
        {
            // Prepare Segments
            // Key: TrackId, Value: List of Segments (StartTcb, EndTcb, StartPos, EndPos)
            var trackSegments = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Segment>>();

            foreach (var track in railml.Infrastructure.Tracks.TrackList)
            {
                var tcbs = track.TrainDetectionElements.TrackCircuitBorders.OrderBy(t => t.Pos).ToList();
                var segments = new System.Collections.Generic.List<Segment>();
                double currentPos = 0;
                TrackCircuitBorder lastTcb = null;

                foreach (var tcb in tcbs)
                {
                    segments.Add(new Segment { TrackId = track.Id, StartPos = currentPos, EndPos = tcb.Pos, StartTcb = lastTcb, EndTcb = tcb });
                    currentPos = tcb.Pos;
                    lastTcb = tcb;
                }
                // Last segment
                double endPos = track.TrackTopology.TrackEnd.Pos; 
                segments.Add(new Segment { TrackId = track.Id, StartPos = currentPos, EndPos = endPos, StartTcb = lastTcb, EndTcb = null });
                
                trackSegments[track.Id] = segments;
            }

            var processedSegments = new System.Collections.Generic.HashSet<Segment>();
            int tvdCount = 1;

            foreach (var track in railml.Infrastructure.Tracks.TrackList)
            {
                foreach (var segment in trackSegments[track.Id])
                {
                    if (processedSegments.Contains(segment)) continue;

                    // Flood Fill for TVD Section
                    var borders = new System.Collections.Generic.HashSet<string>();
                    var q = new System.Collections.Generic.Queue<Segment>();
                    
                    q.Enqueue(segment);
                    processedSegments.Add(segment);

                    while (q.Count > 0)
                    {
                        var curr = q.Dequeue();
                        
                        // Add boundaries
                        if (curr.StartTcb != null) borders.Add(curr.StartTcb.Id);
                        if (curr.EndTcb != null) borders.Add(curr.EndTcb.Id);

                        // If Open End (Start == 0), Traverse
                        if (curr.StartPos == 0)
                        {
                            TraverseConnections(railml, curr.TrackId, true, q, processedSegments, trackSegments);
                        }
                        
                        // If Open End (End == Length), Traverse
                        if (curr.EndTcb == null)
                        {
                            TraverseConnections(railml, curr.TrackId, false, q, processedSegments, trackSegments);
                        }
                    }

                    if (borders.Count > 0)
                    {
                        var tvd = new TvdSection { Id = $"tvd_Switch_{tvdCount}T", Name = $"{tvdCount}T" }; // Naming convention per user example
                        foreach (var bId in borders) tvd.Borders.Add(new BorderRef { Ref = bId });
                        railml.Infrastructure.TvdSections.TvdSectionList.Add(tvd);
                        tvdCount++;
                    }
                }
            }
        }

        private void TraverseConnections(Railml railml, string trackId, bool isBegin, 
            System.Collections.Generic.Queue<Segment> q, 
            System.Collections.Generic.HashSet<Segment> processed,
            System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<Segment>> trackSegments)
        {
            var track = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id == trackId);
            if (track == null) return;

            var node = isBegin ? track.TrackTopology.TrackBegin : track.TrackTopology.TrackEnd;
            if (node == null || node.Connections == null) return;

            // Collect connected Node IDs
            var connectedNodeIds = new System.Collections.Generic.List<string>();
            
            if (node.Connections.ConnectionList != null)
                connectedNodeIds.AddRange(node.Connections.ConnectionList.Select(c => c.Ref));
            
            if (node.Connections.Switches != null)
            {
                foreach(var sw in node.Connections.Switches)
                {
                    connectedNodeIds.Add(sw.Ref);
                    connectedNodeIds.AddRange(sw.ConnectionList.Select(c => c.Ref));
                }
            }

            foreach (var connectedId in connectedNodeIds)
            {
                // Connected ID format: "{TrackId}_begin" or "{TrackId}_end"
                string targetTrackId = connectedId.Replace("_begin", "").Replace("_end", "");
                bool isTargetBegin = connectedId.EndsWith("_begin");

                if (trackSegments.ContainsKey(targetTrackId))
                {
                    // If connecting to Begin, take First segment. If End, take Last segment.
                    var targetSegs = trackSegments[targetTrackId];
                    var nextSeg = isTargetBegin ? targetSegs.First() : targetSegs.Last();

                    if (!processed.Contains(nextSeg))
                    {
                        processed.Add(nextSeg);
                        q.Enqueue(nextSeg);
                    }
                }
            }
        }

        private class Segment
        {
            public string TrackId;
            public double StartPos;
            public double EndPos;
            public TrackCircuitBorder StartTcb;
            public TrackCircuitBorder EndTcb;
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
