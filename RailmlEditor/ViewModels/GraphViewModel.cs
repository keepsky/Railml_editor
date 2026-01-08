#pragma warning disable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels
{
    public class GraphViewModel : ObservableObject
    {
        public ObservableCollection<GraphNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<GraphEdgeViewModel> Edges { get; } = new();

        private Dictionary<string, GraphNodeViewModel> _nodeMap = new();

        private struct TraversalCursor
        {
            public string TrackId;
            public double CurrentPos;
            public bool IsSearchUp; 
        }

        private class TrackPositionNode
        {
            public double Pos { get; set; }
            public GraphNodeViewModel Node { get; set; }
        }

        public void BuildGraph(IEnumerable<BaseElementViewModel> allElements)
        {
            Nodes.Clear();
            Edges.Clear();
            _nodeMap.Clear();

            var trackList = allElements.OfType<TrackViewModel>().ToList();
            var borders = allElements.OfType<TrackCircuitBorderViewModel>().ToList();
            var switches = allElements.OfType<SwitchViewModel>().ToList();
            
            var trackLookup = trackList.ToDictionary(t => t.Id);

            // 1. Create Nodes
            foreach (var track in trackList)
            {
                if (track.BeginNode.NodeType == TrackNodeType.BufferStop || track.BeginNode.NodeType == TrackNodeType.OpenEnd)
                {
                    GetOrCreateNode(track.Id + "_begin", track.BeginNode.NodeType.ToString(), "Terminal", track.X, track.Y);
                }
                if (track.EndNode.NodeType == TrackNodeType.BufferStop || track.EndNode.NodeType == TrackNodeType.OpenEnd)
                {
                    GetOrCreateNode(track.Id + "_end", track.EndNode.NodeType.ToString(), "Terminal", track.X2, track.Y2);
                }

                // Collect Signals from Children
                foreach (var child in track.Children.OfType<SignalViewModel>())
                {
                     GetOrCreateNode(child.Id, child.Name ?? child.Id, "Signal", child.X, child.Y);
                }
            }

            foreach (var sw in switches)
            {
                GetOrCreateNode(sw.Id, sw.Name ?? sw.Id, "Switch", sw.X, sw.Y);
            }

            foreach (var bor in borders)
            {
                GetOrCreateNode(bor.Id, bor.Name ?? bor.Id, "Border", bor.X, bor.Y);
            }

            // 2. Map Nodes to Tracks
            var onTrackNodes = new Dictionary<string, List<TrackPositionNode>>();

            foreach (var node in Nodes)
            {
                if (node.Id.EndsWith("_begin"))
                {
                    string trackId = node.Id.Replace("_begin", "");
                    AddToTrackMap(onTrackNodes, trackId, 0, node);
                }
                else if (node.Id.EndsWith("_end"))
                {
                    string trackId = node.Id.Replace("_end", "");
                    if (trackLookup.TryGetValue(trackId, out var t)) AddToTrackMap(onTrackNodes, trackId, t.Length, node);
                }
                else if (node.Type == "Switch")
                {
                    var sw = switches.FirstOrDefault(s => s.Id == node.Id);
                    if (sw != null)
                    {
                        var connectedTracks = new List<string>();
                        if (!string.IsNullOrEmpty(sw.PrincipleTrackId)) connectedTracks.Add(sw.PrincipleTrackId);
                        if (!string.IsNullOrEmpty(sw.EnteringTrackId) && sw.EnteringTrackId != sw.PrincipleTrackId) connectedTracks.Add(sw.EnteringTrackId);
                        if (sw.DivergingTrackIds != null) connectedTracks.AddRange(sw.DivergingTrackIds);

                        foreach (var rangeTrackId in connectedTracks)
                        {
                            if (trackLookup.TryGetValue(rangeTrackId, out var trk))
                            {
                                // Determine Pos on this track
                                double pos = 0;
                                double dBegin = Math.Sqrt(Math.Pow(trk.X - sw.X, 2) + Math.Pow(trk.Y - sw.Y, 2));
                                double dEnd = Math.Sqrt(Math.Pow(trk.X2 - sw.X, 2) + Math.Pow(trk.Y2 - sw.Y, 2));

                                if (dBegin < 5.0) pos = 0;
                                else if (dEnd < 5.0) pos = trk.Length;
                                else 
                                {
                                    // Mid-track or use sw.Pos if applicable (only if Principle?)
                                    // For robustness, calculate projection or use sw.Pos if track matches
                                    if (rangeTrackId == sw.PrincipleTrackId || rangeTrackId == sw.EnteringTrackId)
                                    {
                                        pos = sw.Pos; // Fallback to assigned Pos (likely correct for single-track switch)
                                    }
                                    else
                                    {
                                        // Simple projection for curved track support might be needed but assuming line segment for now
                                        // If far from ends, maybe use linear interpolation ratio?
                                        // For now, assume sw.Pos is valid if ID matches, else 0/Length close match.
                                        pos = (dBegin < dEnd) ? 0 : trk.Length; // Best guess fallback
                                    }
                                }
                                AddToTrackMap(onTrackNodes, rangeTrackId, pos, node);
                            }
                        }
                    }
                }
                else if (node.Type == "Border")
                {
                    var bor = borders.FirstOrDefault(b => b.Id == node.Id);
                    if (bor != null) AddToTrackMap(onTrackNodes, bor.RelatedTrackId, bor.Pos, node);
                }
                else if (node.Type == "Signal")
                {
                    // Find signal in allElements or track children? 
                    // Better to find it on track children but we flattened list.
                    // We can find it by ID in track children.
                    var sig = trackList.SelectMany(t => t.Children.OfType<SignalViewModel>()).FirstOrDefault(s => s.Id == node.Id);
                    if (sig != null && !string.IsNullOrEmpty(sig.RelatedTrackId))
                    {
                        AddToTrackMap(onTrackNodes, sig.RelatedTrackId, sig.Pos, node);
                    }
                }
            }

            // 3. Create Edges
            var createdEdges = new HashSet<string>();

            foreach (var startNode in Nodes)
            {
                var probes = GetProbesForNode(startNode, trackList, switches, borders);

                foreach (var probe in probes)
                {
                    var target = Traverse(probe, trackLookup, onTrackNodes, startNode);
                    if (target != null && target != startNode)
                    {
                        string edgeKey = GetEdgeKey(startNode.Id, target.Id);
                        if (!createdEdges.Contains(edgeKey))
                        {
                            Edges.Add(new GraphEdgeViewModel(startNode, target, "none")); 
                            createdEdges.Add(edgeKey);
                        }
                    }
                }
            }
        }

        private void AddToTrackMap(Dictionary<string, List<TrackPositionNode>> map, string trackId, double pos, GraphNodeViewModel node)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            if (!map.ContainsKey(trackId)) map[trackId] = new List<TrackPositionNode>();
            map[trackId].Add(new TrackPositionNode { Pos = pos, Node = node});
        }

        private List<TraversalCursor> GetProbesForNode(GraphNodeViewModel node, List<TrackViewModel> tracks, List<SwitchViewModel> switches, List<TrackCircuitBorderViewModel> borders)
        {
            var probes = new List<TraversalCursor>();

            if (node.Type == "Terminal")
            {
                if (node.Id.EndsWith("_begin"))
                {
                    string tid = node.Id.Replace("_begin", "");
                    probes.Add(new TraversalCursor { TrackId = tid, CurrentPos = 0, IsSearchUp = true });
                }
                else if (node.Id.EndsWith("_end"))
                {
                    string tid = node.Id.Replace("_end", "");
                    var trk = tracks.FirstOrDefault(x => x.Id == tid);
                    if (trk != null) probes.Add(new TraversalCursor { TrackId = tid, CurrentPos = trk.Length, IsSearchUp = false });
                }
            }
            else if (node.Type == "Switch")
            {
                var sw = switches.FirstOrDefault(s => s.Id == node.Id);
                if (sw != null && !string.IsNullOrEmpty(sw.PrincipleTrackId))
                {
                    string pId = sw.PrincipleTrackId;
                    probes.Add(new TraversalCursor { TrackId = pId, CurrentPos = sw.Pos, IsSearchUp = true });
                    probes.Add(new TraversalCursor { TrackId = pId, CurrentPos = sw.Pos, IsSearchUp = false });
                
                    foreach (var div in sw.DivergingConnections)
                    {
                        var divTrack = tracks.FirstOrDefault(t => t.Id == div.TrackId);
                        if (divTrack != null)
                        {
                            bool added = false;
                            if (!string.IsNullOrEmpty(div.Ref))
                            {
                                // Ref might be "cbX" or "ceX" or "tbX" or "teX"
                                // Check if Ref matches BeginNode ConnectionId or Id
                                if (div.Ref == divTrack.BeginNode.ConnectionId || div.Ref == divTrack.BeginNode.Id || div.Ref.EndsWith("_begin")) 
                                {
                                    probes.Add(new TraversalCursor { TrackId = div.TrackId, CurrentPos = 0, IsSearchUp = true });
                                    added = true;
                                }
                                // Check if Ref matches EndNode ConnectionId or Id
                                else if (div.Ref == divTrack.EndNode.ConnectionId || div.Ref == divTrack.EndNode.Id || div.Ref.EndsWith("_end"))
                                {
                                    probes.Add(new TraversalCursor { TrackId = div.TrackId, CurrentPos = divTrack.Length, IsSearchUp = false });
                                    added = true;
                                }
                            }

                            if (!added)
                            {
                                double d1 = Math.Sqrt(Math.Pow(divTrack.X - sw.X, 2) + Math.Pow(divTrack.Y - sw.Y, 2));
                                if (d1 < 5.0) probes.Add(new TraversalCursor { TrackId = div.TrackId, CurrentPos = 0, IsSearchUp = true });

                                double d2 = Math.Sqrt(Math.Pow(divTrack.X2 - sw.X, 2) + Math.Pow(divTrack.Y2 - sw.Y, 2));
                                if (d2 < 5.0) probes.Add(new TraversalCursor { TrackId = div.TrackId, CurrentPos = divTrack.Length, IsSearchUp = false });
                            }
                        }
                    }
                }
            }
            else if (node.Type == "Border")
            {
                var bor = borders.FirstOrDefault(b => b.Id == node.Id);
                if (bor != null)
                {
                    probes.Add(new TraversalCursor { TrackId = bor.RelatedTrackId, CurrentPos = bor.Pos, IsSearchUp = true });
                    probes.Add(new TraversalCursor { TrackId = bor.RelatedTrackId, CurrentPos = bor.Pos, IsSearchUp = false });
                }
            }
            else if (node.Type == "Signal")
            {
               var sig = tracks.SelectMany(t => t.Children.OfType<SignalViewModel>()).FirstOrDefault(s => s.Id == node.Id);
               if (sig != null && !string.IsNullOrEmpty(sig.RelatedTrackId))
               {
                   probes.Add(new TraversalCursor { TrackId = sig.RelatedTrackId, CurrentPos = sig.Pos, IsSearchUp = true });
                   probes.Add(new TraversalCursor { TrackId = sig.RelatedTrackId, CurrentPos = sig.Pos, IsSearchUp = false });
               }
            }
            return probes;
        }

        private GraphNodeViewModel? Traverse(TraversalCursor cursor, Dictionary<string, TrackViewModel> trackLookup, Dictionary<string, List<TrackPositionNode>> onTrackNodes, GraphNodeViewModel startNode, int depth = 0)
        {
            if (depth > 20) return null; // Prevent infinite recursion

            if (!trackLookup.TryGetValue(cursor.TrackId, out var track)) return null;

            if (!onTrackNodes.TryGetValue(cursor.TrackId, out var nodesOnTrack)) nodesOnTrack = new List<TrackPositionNode>();

            nodesOnTrack.Sort((a, b) => a.Pos.CompareTo(b.Pos));

            TrackPositionNode? nextNode = null;

            if (cursor.IsSearchUp)
            {
                var candidates = nodesOnTrack.Where(n => n.Pos >= cursor.CurrentPos - 0.001).OrderBy(n => n.Pos);
                foreach(var c in candidates)
                {
                     if (c.Node != startNode) { nextNode = c; break; }
                }

                if (nextNode != null && nextNode.Node != null) return nextNode.Node;
                
                return CheckConnections(track, track.Length, trackLookup, onTrackNodes, startNode, depth);
            }
            else
            {
                var candidates = nodesOnTrack.Where(n => n.Pos <= cursor.CurrentPos + 0.001).OrderByDescending(n => n.Pos);
                foreach(var c in candidates)
                {
                     if (c.Node != startNode) { nextNode = c; break; }
                }

                if (nextNode != null && nextNode.Node != null) return nextNode.Node;

                return CheckConnections(track, 0, trackLookup, onTrackNodes, startNode, depth);
            }
        }

        private GraphNodeViewModel? CheckConnections(TrackViewModel track, double pos, Dictionary<string, TrackViewModel> trackLookup, Dictionary<string, List<TrackPositionNode>> onTrackNodes, GraphNodeViewModel startNode, int depth)
        {
            // Determine if at Begin or End
            bool isBegin = Math.Abs(pos) < 0.001;
            bool isEnd = Math.Abs(pos - track.Length) < 0.001;

            if (isBegin)
            {
                if (track.BeginNode.NodeType == TrackNodeType.Connection && !string.IsNullOrEmpty(track.BeginNode.ConnectedTrackId))
                {
                    // Check connected track
                     if (trackLookup.TryGetValue(track.BeginNode.ConnectedTrackId, out var targetTrack))
                     {
                         // Determine entry position on target track. 
                         string refId = track.BeginNode.ConnectedNodeId;
                         
                         bool enterAtZero = true; // Default
                         
                         if (refId == targetTrack.EndNode.ConnectionId || refId == targetTrack.EndNode.Id || refId == targetTrack.EndNode.ConnectionRef)
                         {
                             enterAtZero = false; 
                         }
                         else if (refId == targetTrack.BeginNode.ConnectionId || refId == targetTrack.BeginNode.Id || refId == targetTrack.BeginNode.ConnectionRef)
                         {
                             enterAtZero = true;
                         }
                         else if (!string.IsNullOrEmpty(refId) && (refId.Contains("te") || refId.Contains("_end")))
                         {
                             enterAtZero = false;
                         }

                         // Continue Traversal
                         var newCursor = new TraversalCursor 
                         { 
                             TrackId = targetTrack.Id, 
                             CurrentPos = enterAtZero ? 0 : targetTrack.Length, 
                             IsSearchUp = enterAtZero 
                         };

                         return Traverse(newCursor, trackLookup, onTrackNodes, startNode, depth + 1);
                     }
                }
                else if (track.BeginNode.NodeType == TrackNodeType.BufferStop || track.BeginNode.NodeType == TrackNodeType.OpenEnd)
                {
                    // Return the terminal node if it exists
                    if (onTrackNodes.TryGetValue(track.Id, out var nodes))
                    {
                        var termNode = nodes.FirstOrDefault(n => Math.Abs(n.Pos) < 0.001 && n.Node.Type == "Terminal");
                        if (termNode != null && termNode.Node != startNode) return termNode.Node;
                    }
                }
            }
            else if (isEnd)
            {
                if (track.EndNode.NodeType == TrackNodeType.Connection && !string.IsNullOrEmpty(track.EndNode.ConnectedTrackId))
                {
                     if (trackLookup.TryGetValue(track.EndNode.ConnectedTrackId, out var targetTrack))
                     {
                         string refId = track.EndNode.ConnectedNodeId;
                         bool enterAtZero = true;
                         
                         if (refId == targetTrack.EndNode.ConnectionId || refId == targetTrack.EndNode.Id || refId == targetTrack.EndNode.ConnectionRef)
                         {
                             enterAtZero = false; 
                         }
                         else if (refId == targetTrack.BeginNode.ConnectionId || refId == targetTrack.BeginNode.Id || refId == targetTrack.BeginNode.ConnectionRef)
                         {
                             enterAtZero = true;
                         }
                         else if (!string.IsNullOrEmpty(refId) && (refId.Contains("te") || refId.Contains("_end")))
                         {
                             enterAtZero = false;
                         }

                         var newCursor = new TraversalCursor 
                         { 
                             TrackId = targetTrack.Id, 
                             CurrentPos = enterAtZero ? 0 : targetTrack.Length, 
                             IsSearchUp = enterAtZero 
                         };

                         return Traverse(newCursor, trackLookup, onTrackNodes, startNode, depth + 1);
                     }
                }
                else if (track.EndNode.NodeType == TrackNodeType.BufferStop || track.EndNode.NodeType == TrackNodeType.OpenEnd)
                {
                    // Return the terminal node if it exists
                    if (onTrackNodes.TryGetValue(track.Id, out var nodes))
                    {
                        var termNode = nodes.FirstOrDefault(n => Math.Abs(n.Pos - track.Length) < 0.001 && n.Node.Type == "Terminal");
                        if (termNode != null && termNode.Node != startNode) return termNode.Node;
                    }
                }
            }
            return null;
        }

        private string GetEdgeKey(string id1, string id2)
        {
             return string.Compare(id1, id2) < 0 ? $"{id1}-{id2}" : $"{id2}-{id1}";
        }

        private GraphNodeViewModel GetOrCreateNode(string id, string label, string type, double x, double y)
        {
            if (_nodeMap.TryGetValue(id, out var existing)) return existing;
            
            var node = new GraphNodeViewModel(id, label, type, x, y);
            Nodes.Add(node);
            _nodeMap[id] = node;
            return node;
        }
    }
}
