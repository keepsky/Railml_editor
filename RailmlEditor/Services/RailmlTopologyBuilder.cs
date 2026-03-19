using System;
using System.Linq;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    /// <summary>
    /// RailML 파일로 저장하기 직전에, 화면 위의 요소들이 어떻게 이어져 있는지를 계산해서 
    /// RailML 규격의 XML 구조(Connections, Topology)로 변환해주는 전담 건축가(Builder) 클래스입니다.
    /// </summary>
    public static class RailmlTopologyBuilder
    {
        public static void BuildTopology(Railml railml, DocumentViewModel doc)
        {
            var allNodes = new System.Collections.Generic.List<TrackNode>();
            var nodeToTrack = new System.Collections.Generic.Dictionary<TrackNode, Track>(); // Map Node -> Track

            foreach(var track in railml.Infrastructure.Tracks.TrackList)
            {
                if (track.TrackTopology?.TrackBegin != null)
                {
                    allNodes.Add(track.TrackTopology.TrackBegin);
                    nodeToTrack[track.TrackTopology.TrackBegin] = track;
                }

                if (track.TrackTopology?.TrackEnd != null)
                {
                    allNodes.Add(track.TrackTopology.TrackEnd);
                    nodeToTrack[track.TrackTopology.TrackEnd] = track;
                }
            }

            foreach(var nodeA in allNodes)
            {
                double ax = nodeA.ScreenPos?.X ?? 0;
                double ay = nodeA.ScreenPos?.Y ?? 0;
                
                var overlappingNodes = new System.Collections.Generic.List<TrackNode>();
                foreach(var nodeB in allNodes)
                {
                    double bx = nodeB.ScreenPos?.X ?? 0;
                    double by = nodeB.ScreenPos?.Y ?? 0;

                    double dist = Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
                    if(dist < RailmlEditor.Models.AppSettings.Instance.NodeMappingTolerance) overlappingNodes.Add(nodeB);
                }

                if (overlappingNodes.Count > 0)
                {
                    var parentTrack = nodeToTrack[nodeA];
                    bool isBeginA = nodeA.Id != null && nodeA.Id.StartsWith("tb");

                    if (overlappingNodes.Count == 1)
                    {
                         foreach (var overlappingNode in overlappingNodes)
                         {
                             bool isBeginB = overlappingNode.Id != null && overlappingNode.Id.StartsWith("tb");
                             // if (isBeginA == isBeginB) continue; // Allow Begin-Begin and End-End connections 

                             var targetTrack = nodeToTrack[overlappingNode];
                             string targetTrackPart = System.Text.RegularExpressions.Regex.Match(targetTrack.Id, @"\d+").Value;
                             string targetConnId = (isBeginB ? "cb" : "ce") + targetTrackPart;

                             if(!nodeA.ConnectionList.Any(c => c.Ref == targetConnId))
                             {
                                 string trackPart = System.Text.RegularExpressions.Regex.Match(parentTrack.Id, @"\d+").Value;
                                 string connPrefix = isBeginA ? "cb" : "ce";
                                 
                                 nodeA.ConnectionList.Add(new Connection 
                                 { 
                                     Id = connPrefix + trackPart,
                                     Ref = targetConnId 
                                 });
                             }
                         }
                        // If a node has only one overlapping node, and it's not connected, it's an open end or buffer stop
                        var trackVm = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == parentTrack.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(parentTrack.Id ?? "", @"\d+").Value);
                        if (trackVm != null)
                        {
                            if (isBeginA)
                            {
                                trackVm.BeginType = TrackNodeType.BufferStop;
                                trackVm.BeginNode.Code = "bufferStop";
                                trackVm.BeginNode.Name = $"End({trackVm.Name})";
                            }
                            else
                            {
                                trackVm.EndType = TrackNodeType.OpenEnd;
                                trackVm.EndNode.Code = "openEnd";
                                trackVm.EndNode.Name = $"OEnd({trackVm.Name})";
                            }
                        }
                    }
                    else if (overlappingNodes.Count == 2)
                    {
                        // 2 nodes -> 2 tracks meeting = "joint"
                        var otherNode = overlappingNodes.First(n => n != nodeA);
                        var otherTrack = nodeToTrack[otherNode];
                        bool isBeginB = otherNode.Id != null && otherNode.Id.StartsWith("tb");

                        var trackVmA = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == parentTrack.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(parentTrack.Id ?? "", @"\d+").Value);
                        var trackVmB = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == otherTrack.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(otherTrack.Id ?? "", @"\d+").Value);
                    }
                    else if (overlappingNodes.Count == 3) // This is a switch
                    {
                        // Switch mapping (3 nodes meeting)
                        var otherNodes = overlappingNodes.Where(n => n != nodeA).ToList();
                        var otherTrack1 = nodeToTrack[otherNodes[0]];
                        var otherTrack2 = nodeToTrack[otherNodes[1]];

                        // Try to find an existing switch in the ViewModel at this screen position
                        var sw = doc.Elements.OfType<SwitchViewModel>().FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - ax, 2) + Math.Pow(s.Y - ay, 2)) < RailmlEditor.Models.AppSettings.Instance.NodeMappingTolerance);
                        if (sw != null)
                        {
                            // Assign branches
                            var trackA = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == parentTrack.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(parentTrack.Id ?? "", @"\d+").Value);
                            var trackB = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == otherTrack1.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(otherTrack1.Id ?? "", @"\d+").Value);
                            var trackC = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == otherTrack2.Id || t.Id == "tr" + System.Text.RegularExpressions.Regex.Match(otherTrack2.Id ?? "", @"\d+").Value);
                        }
                    }
                    else
                    {
                         var swVm = doc.Elements.OfType<SwitchViewModel>()
                                 .FirstOrDefault(s => {
                                     var swTracks = new System.Collections.Generic.HashSet<string?>();
                                     if (!string.IsNullOrEmpty(s.EnteringTrackId)) swTracks.Add(GetRailmlId(s.EnteringTrackId));
                                     if (!string.IsNullOrEmpty(s.PrincipleTrackId)) swTracks.Add(GetRailmlId(s.PrincipleTrackId));
                                     foreach (var id in s.DivergingTrackIds) if (!string.IsNullOrEmpty(id)) swTracks.Add(GetRailmlId(id));

                                     var clusterTracks = new System.Collections.Generic.HashSet<string?>(overlappingNodes.Select(n => nodeToTrack[n].Id));
                                     clusterTracks.Add(parentTrack.Id);
                                     
                                     return swTracks.Count > 0 && swTracks.SetEquals(clusterTracks);
                                 });
                         
                         if (swVm != null)
                         {
                             // Find the entering, principle, and diverging tracks in this cluster
                             // We already have swVm.EnteringTrackId, swVm.PrincipleTrackId, swVm.DivergingTrackIds
                             
                             bool isEnteringNode = (parentTrack.Id == GetRailmlId(swVm.EnteringTrackId));
                             bool isPrincipleNode = (parentTrack.Id == GetRailmlId(swVm.PrincipleTrackId));

                             // (1-d) & (2-d) bidirectional connection between entering and principle
                             if (isEnteringNode || isPrincipleNode)
                             {
                                 var targetId = isEnteringNode ? "t" + (swVm.IsScenario1 ? "b" : "e") + System.Text.RegularExpressions.Regex.Match(GetRailmlId(swVm.PrincipleTrackId), @"\d+").Value 
                                                               : "t" + (swVm.IsScenario1 ? "e" : "b") + System.Text.RegularExpressions.Regex.Match(GetRailmlId(swVm.EnteringTrackId), @"\d+").Value;
                                 
                                  if (!nodeA.ConnectionList.Any(c => c.Ref == targetId))
                                 {
                                     string trackPart = System.Text.RegularExpressions.Regex.Match(parentTrack.Id, @"\d+").Value;
                                     string connPrefix = isBeginA ? "cb" : "ce";
                                     
                                     // targetId is a node ID like tb2 or te2. We need cb2 or ce2.
                                     string targetConnId = targetId.Replace("tb", "cb").Replace("te", "ce");

                                     nodeA.ConnectionList.Add(new Connection 
                                     { 
                                         Id = connPrefix + trackPart,
                                         Ref = targetConnId 
                                     });
                                 }
                             }

                             // (1-e) & (2-e) Identify where the <switch> tag goes
                             bool shouldHostSwitch = swVm.IsScenario1 ? isPrincipleNode : isEnteringNode;
                             if (shouldHostSwitch)
                             {
                                 if (parentTrack.TrackTopology!.Connections == null) 
                                     parentTrack.TrackTopology.Connections = new Connections();

                                 if (!parentTrack.TrackTopology.Connections.Switches.Any(s => s.Id == swVm.Id))
                                 {
                                     var switchObj = new Switch
                                     {
                                         Id = swVm.Id,
                                         Pos = 0, // as requested
                                         TrackContinueCourse = swVm.TrackContinueCourse,
                                         NormalPosition = swVm.NormalPosition,
                                         AdditionalName = new AdditionalName { Name = swVm.Name },
                                         ScreenPos = new ScreenPos { X = swVm.X, XSpecified = true, Y = swVm.Y, YSpecified = true }
                                     };

                                     // (1-f) & (2-f) adding diverging connections
                                     foreach (var divId in swVm.DivergingTrackIds)
                                     {
                                          var divTrackNum = System.Text.RegularExpressions.Regex.Match(GetRailmlId(divId), @"\d+").Value;
                                         var divRefId = (swVm.IsScenario1 ? "cb" : "ce") + divTrackNum;
                                         
                                         var connVm = swVm.DivergingConnections.FirstOrDefault(dc => dc.TrackId == divId);

                                          var switchPart = System.Text.RegularExpressions.Regex.Match(swVm.Id, @"\d+").Value;
                                         var swConnId = $"c{switchPart}-{divRefId}";
                                         switchObj.ConnectionList.Add(new Connection
                                         {
                                             Id = swConnId,
                                             Ref = divRefId,
                                             Orientation = !string.IsNullOrEmpty(connVm?.Orientation) ? connVm.Orientation : (swVm.IsScenario1 ? "outgoing" : "incoming"),
                                             Course = connVm?.Course ?? "straight"
                                         });

                                         // Add reciprocal connection to diverging track node
                                         var divTrackObj = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id == GetRailmlId(divId));
                                         if (divTrackObj != null)
                                         {
                                             var targetNode = swVm.IsScenario1 ? divTrackObj.TrackTopology!.TrackBegin : divTrackObj.TrackTopology!.TrackEnd;
                                             if (targetNode != null)
                                             {
                                                 if (!targetNode.ConnectionList.Any(c => c.Ref == swConnId))
                                                 {
                                                     targetNode.ConnectionList.Add(new Connection
                                                     {
                                                         Id = divRefId,
                                                         Ref = swConnId
                                                     });
                                                 }
                                             }
                                         }
                                     }
                                     parentTrack.TrackTopology.Connections.Switches.Add(switchObj);
                                 }
                             }
                         }
                    }
                }
            }

        }
        
        private static string GetRailmlId(string? originalId)
        {
            if (string.IsNullOrEmpty(originalId)) return "unknown";
            return originalId.ToLowerInvariant().Replace("tr", "t").Replace("sig", "s").Replace("sw", "is");
        }
    }
}