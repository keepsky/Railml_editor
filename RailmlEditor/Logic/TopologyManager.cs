using System;
using System.Collections.Generic;
using System.Linq;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Logic
{
    public class TopologyManager
    {
        // Event to request user selection (e.g. Principle switch branch)
        public event Action<SwitchBranchInfo> PrincipleTrackSelectionRequested;

        public void CheckConnections(TrackViewModel source, IEnumerable<BaseElementViewModel> allElements)
        {
            // Check Begin Node
            CheckNodeConnection(source, source.BeginNode, source.X, source.Y, allElements);
            // Check End Node
            CheckNodeConnection(source, source.EndNode, source.X2, source.Y2, allElements);
        }

        private void CheckNodeConnection(TrackViewModel sourceTrack, TrackNodeViewModel sourceNode, double x, double y, IEnumerable<BaseElementViewModel> allElements)
        {
            double tolerance = 1.0;
            TrackNodeViewModel? nearestNode = null;
            TrackViewModel? nearestTrack = null;
            double minDist = double.MaxValue;

            var tracks = allElements.OfType<TrackViewModel>();
            var switches = allElements.OfType<SwitchViewModel>();

            foreach (var other in tracks)
            {
                if (other == sourceTrack) continue;

                // Check other Begin
                double d1 = Math.Sqrt(Math.Pow(other.X - x, 2) + Math.Pow(other.Y - y, 2));
                if (d1 < minDist && d1 < tolerance)
                {
                    minDist = d1;
                    nearestTrack = other;
                    nearestNode = other.BeginNode;
                }

                // Check other End
                double d2 = Math.Sqrt(Math.Pow(other.X2 - x, 2) + Math.Pow(other.Y2 - y, 2));
                if (d2 < minDist && d2 < tolerance)
                {
                    minDist = d2;
                    nearestTrack = other;
                    nearestNode = other.EndNode;
                }
            }

            // Also check Switches
            SwitchViewModel? nearestSwitch = null;
            foreach (var sw in switches)
            {
                double dist = Math.Sqrt(Math.Pow(sw.X - x, 2) + Math.Pow(sw.Y - y, 2));
                if (dist < minDist && dist < tolerance)
                {
                    minDist = dist;
                    nearestSwitch = sw;
                    nearestTrack = null;
                    nearestNode = null;
                }
            }

            if (nearestTrack != null && nearestNode != null)
            {
                // Connect to track
                sourceNode.NodeType = TrackNodeType.Connection;
                sourceNode.ConnectedTrackId = nearestTrack.Id;
                sourceNode.ConnectedNodeId = nearestNode.ConnectionId; // Ref to Connection ID (cb/ce)

                // Bidirectional
                nearestNode.NodeType = TrackNodeType.Connection;
                nearestNode.ConnectedTrackId = sourceTrack.Id;
                nearestNode.ConnectedNodeId = sourceNode.ConnectionId;
            }
            else if (nearestSwitch != null)
            {
                // Connect to switch
                sourceNode.NodeType = TrackNodeType.Connection;
                sourceNode.ConnectedTrackId = nearestSwitch.Id;
                sourceNode.ConnectedNodeId = nearestSwitch.Id; // Ref to switch ID temporarily
            }
            else
            {
                if (sourceNode.NodeType == TrackNodeType.Connection)
                {
                    sourceNode.NodeType = TrackNodeType.None;
                    sourceNode.ConnectedTrackId = null;
                    sourceNode.ConnectedNodeId = null;
                }
            }
        }

        public void UpdateProximitySwitches(IList<BaseElementViewModel> elements)
        {
            var tracks = elements.OfType<TrackViewModel>().ToList();
            var points = new List<(double X, double Y, string TrackId, bool isEnd)>();

            foreach (var t in tracks)
            {
                points.Add((t.X, t.Y, t.Id, false));
                points.Add((t.X2, t.Y2, t.Id, true));
            }

            var clusters = new List<(double X, double Y, List<(string TrackId, bool isEnd)> Members)>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (processedIndices.Contains(i)) continue;

                var members = new List<(string TrackId, bool isEnd)> { (points[i].TrackId, points[i].isEnd) };
                double sumX = points[i].X;
                double sumY = points[i].Y;
                processedIndices.Add(i);

                for (int j = i + 1; j < points.Count; j++)
                {
                    if (processedIndices.Contains(j)) continue;

                    double dist = Math.Sqrt(Math.Pow(points[i].X - points[j].X, 2) + Math.Pow(points[i].Y - points[j].Y, 2));
                    if (dist < 10.0) // Clustering threshold
                    {
                        sumX += points[j].X;
                        sumY += points[j].Y;
                        members.Add((points[j].TrackId, points[j].isEnd));
                        processedIndices.Add(j);
                    }
                }

                if (members.Count > 2)
                {
                    int endCount = members.Count(m => m.isEnd);
                    int beginCount = members.Count(m => !m.isEnd);

                    if ((endCount == 1 && beginCount >= 2) || (beginCount == 1 && endCount >= 2))
                    {
                        clusters.Add((sumX / members.Count, sumY / members.Count, members));
                    }
                }
            }

            var existingSwitches = elements.OfType<SwitchViewModel>().ToList();
            var switchesToRemove = existingSwitches.Where(sw => !clusters.Any(c => Math.Sqrt(Math.Pow(sw.X - c.X, 2) + Math.Pow(sw.Y - c.Y, 2)) < 10.0)).ToList();

            foreach (var sw in switchesToRemove) elements.Remove(sw);

            // Find Max ID using IdGenerator logic locally or passing generator? 
            // Better to implement simple max logic here or inject IdGenerator.
            // For now, inline logic similar to before but correct.
            int maxId = 0;
            foreach (var sw in elements.OfType<SwitchViewModel>())
            {
                 if (sw.Id.StartsWith("sw") && int.TryParse(sw.Id.Substring(2), out int num))
                    if (num > maxId) maxId = num;
            }

            foreach (var c in clusters)
            {
                var sw = elements.OfType<SwitchViewModel>().FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - c.X, 2) + Math.Pow(s.Y - c.Y, 2)) < 10.0);
                if (sw == null)
                {
                    maxId++;
                    sw = new SwitchViewModel
                    {
                        Id = $"sw{maxId}",
                        Name = $"sw{maxId}",
                        X = c.X,
                        Y = c.Y
                    };

                    int endCount = c.Members.Count(m => m.isEnd);
                    sw.IsScenario1 = (endCount == 1);

                    var enteringMember = sw.IsScenario1 ? c.Members.First(m => m.isEnd) : c.Members.First(m => !m.isEnd);
                    var candidateMembers = sw.IsScenario1 ? c.Members.Where(m => !m.isEnd).ToList() : c.Members.Where(m => m.isEnd).ToList();

                    var candidates = candidateMembers.Select(m => elements.OfType<TrackViewModel>().First(t => t.Id == m.TrackId)).ToList();

                    PrincipleTrackSelectionRequested?.Invoke(new SwitchBranchInfo
                    {
                        Switch = sw,
                        Candidates = candidates,
                        Callback = (principle) =>
                        {
                            if (principle != null)
                            {
                                sw.EnteringTrackId = enteringMember.TrackId;
                                sw.PrincipleTrackId = principle.Id;
                                sw.DivergingTrackIds.Clear();
                                sw.DivergingConnections.Clear();
                                foreach (var cand in candidates)
                                {
                                    if (cand.Id != principle.Id)
                                    {
                                        sw.DivergingTrackIds.Add(cand.Id);
                                        sw.DivergingConnections.Add(new DivergingConnectionViewModel
                                        {
                                            TrackId = cand.Id,
                                            TargetTrack = cand,
                                            Orientation = sw.IsScenario1 ? "outgoing" : "incoming"
                                        });
                                    }
                                }
                                elements.Add(sw);
                            }
                        }
                    });
                }
            }
        }
        
        public void UpdateTrackNodesToSwitch(SwitchViewModel sw, IEnumerable<BaseElementViewModel> allElements)
        {
             // Logic from MainViewModel...
             string swNum = System.Text.RegularExpressions.Regex.Match(sw.Id, @"\d+").Value;
             
             UpdateTrackNode(sw.EnteringTrackId, !sw.IsScenario1, sw.X, sw.Y, sw.Id, sw.Id, allElements);
             UpdateTrackNode(sw.PrincipleTrackId, sw.IsScenario1, sw.X, sw.Y, sw.Id, sw.Id, allElements);
             
             foreach (var divId in sw.DivergingTrackIds)
             {
                 var divTrack = allElements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == divId);
                 string? connId = null;
                 string? targetRef = null;
                 if (divTrack != null)
                 {
                     var node = sw.IsScenario1 ? divTrack.BeginNode : divTrack.EndNode;
                     connId = $"c{swNum}-{node.ConnectionId}";
                     targetRef = node.ConnectionId;
                 }
                 UpdateTrackNode(divId, sw.IsScenario1, sw.X, sw.Y, sw.Id, connId ?? sw.Id, allElements);
                 
                 // Update DivergingConnectionViewModel details
                 var connVm = sw.DivergingConnections.FirstOrDefault(dc => dc.TrackId == divId);
                 if (connVm != null)
                 {
                     connVm.Id = connId ?? "";
                     connVm.Ref = targetRef ?? "";
                     connVm.Orientation = sw.IsScenario1 ? "outgoing" : "incoming";
                 }
             }
        }

        private void UpdateTrackNode(string? trackId, bool isBegin, double x, double y, string targetId, string? targetConnId, IEnumerable<BaseElementViewModel> allElements)
        {
            if (string.IsNullOrEmpty(trackId)) return;
            var track = allElements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == trackId);
            if (track == null) return;

            if (isBegin)
            {
                if (Math.Abs(track.X - x) > 0.001 || Math.Abs(track.Y - y) > 0.001)
                {
                    track.X = x;
                    track.Y = y;
                }
                track.BeginNode.NodeType = TrackNodeType.Connection;
                track.BeginNode.ConnectedTrackId = targetId;
                track.BeginNode.ConnectedNodeId = targetConnId;
            }
            else
            {
                if (Math.Abs(track.X2 - x) > 0.001 || Math.Abs(track.Y2 - y) > 0.001)
                {
                    track.X2 = x;
                    track.Y2 = y;
                }
                track.EndNode.NodeType = TrackNodeType.Connection;
                track.EndNode.ConnectedTrackId = targetId;
                track.EndNode.ConnectedNodeId = targetConnId;
            }
        }
    }
}
