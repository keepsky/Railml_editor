using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RailmlEditor.ViewModels
{
    public class GraphViewModel : ObservableObject
    {
        public ObservableCollection<GraphNodeViewModel> Nodes { get; } = new();
        public ObservableCollection<GraphEdgeViewModel> Edges { get; } = new();

        private Dictionary<string, GraphNodeViewModel> _nodeMap = new();
        private Dictionary<string, List<(double Pos, GraphNodeViewModel Node)>> _trackNodeCollections = new();

        public void BuildGraph(IEnumerable<BaseElementViewModel> allElements)
        {
            Nodes.Clear();
            Edges.Clear();
            _nodeMap.Clear();
            _trackNodeCollections.Clear();

            var trackList = allElements.OfType<TrackViewModel>().ToList();
            var signals = allElements.OfType<SignalViewModel>().ToList();
            var borders = allElements.OfType<TrackCircuitBorderViewModel>().ToList();
            var switches = allElements.OfType<SwitchViewModel>().ToList();

            var trackLookup = trackList.ToDictionary(t => t.Id);

            // 1. Create Nodes
            foreach (var track in trackList)
            {
                var trackNodes = new List<(double Pos, GraphNodeViewModel Node)>();

                // (1-1) trackBegin / trackEnd with openEnd or bufferStop
                if (track.BeginType == TrackNodeType.OpenEnd || track.BeginType == TrackNodeType.BufferStop)
                {
                    var node = GetOrCreateNode(track.Id + "_begin", track.BeginType.ToString(), "Terminal", track.X, track.Y);
                    trackNodes.Add((0, node));
                }

                if (track.EndType == TrackNodeType.OpenEnd || track.EndType == TrackNodeType.BufferStop)
                {
                    var node = GetOrCreateNode(track.Id + "_end", track.EndType.ToString(), "Terminal", track.X2, track.Y2);
                    trackNodes.Add((track.Length, node));
                }

                // (1-2) Internal nodes: Signals, Borders, Switches
                foreach (var sig in signals.Where(s => s.RelatedTrackId == track.Id))
                {
                    var node = GetOrCreateNode(sig.Id, sig.Name ?? sig.Id, "Signal", sig.X, sig.Y);
                    trackNodes.Add((sig.Pos, node));
                }

                foreach (var bor in borders.Where(b => b.RelatedTrackId == track.Id))
                {
                    var node = GetOrCreateNode(bor.Id, bor.Name ?? bor.Id, "Border", bor.X, bor.Y);
                    trackNodes.Add((bor.Pos, node));
                }

                foreach (var sw in switches)
                {
                    if (IsAtPoint(sw.X, sw.Y, track.X, track.Y))
                    {
                        var node = GetOrCreateNode(sw.Id, sw.Name ?? sw.Id, "Switch", sw.X, sw.Y);
                        trackNodes.Add((0, node));
                    }
                    else if (IsAtPoint(sw.X, sw.Y, track.X2, track.Y2))
                    {
                        var node = GetOrCreateNode(sw.Id, sw.Name ?? sw.Id, "Switch", sw.X, sw.Y);
                        trackNodes.Add((track.Length, node));
                    }
                }

                // (1-3) Sort and Connect Internally
                var sortedNodes = trackNodes.OrderBy(n => n.Pos).ToList();
                _trackNodeCollections[track.Id] = sortedNodes;

                var distinctSortedNodes = sortedNodes.Select(n => n.Node).Distinct().ToList();
                for (int i = 0; i < distinctSortedNodes.Count - 1; i++)
                {
                    var edge = new GraphEdgeViewModel(distinctSortedNodes[i], distinctSortedNodes[i + 1], track.MainDir);
                    Edges.Add(edge);
                }
            }

            // 2. Inter-track Connections (Revised Rules)
            foreach (var track in trackList)
            {
                ConnectEndpoints(track, track.BeginNode, 0, trackLookup);
                ConnectEndpoints(track, track.EndNode, track.Length, trackLookup);
            }
        }

        private void ConnectEndpoints(TrackViewModel track, TrackNodeViewModel nodeVm, double anchorPos, Dictionary<string, TrackViewModel> trackLookup)
        {
            if (nodeVm.ConnectedTrackId != null && trackLookup.TryGetValue(nodeVm.ConnectedTrackId, out var targetTrack))
            {
                var nearestOnThis = GetNearestNode(track.Id, anchorPos);
                
                double targetAnchorPos = 0;
                if (nodeVm.ConnectedNodeId != null && nodeVm.ConnectedNodeId.EndsWith("_end"))
                {
                    targetAnchorPos = targetTrack.Length;
                }
                
                var nearestOnTarget = GetNearestNode(targetTrack.Id, targetAnchorPos);
                
                if (nearestOnThis != null && nearestOnTarget != null && nearestOnThis != nearestOnTarget)
                {
                    // Avoid duplicate edges
                    if (!Edges.Any(e => (e.FromNode == nearestOnThis && e.ToNode == nearestOnTarget) || 
                                       (e.FromNode == nearestOnTarget && e.ToNode == nearestOnThis)))
                    {
                        Edges.Add(new GraphEdgeViewModel(nearestOnThis, nearestOnTarget, "none"));
                    }
                }
            }
        }

        private GraphNodeViewModel? GetNearestNode(string trackId, double targetPos)
        {
            if (!_trackNodeCollections.TryGetValue(trackId, out var nodes) || nodes.Count == 0) return null;
            return nodes.OrderBy(n => Math.Abs(n.Pos - targetPos)).First().Node;
        }

        private GraphNodeViewModel GetOrCreateNode(string id, string label, string type, double x, double y)
        {
            if (_nodeMap.TryGetValue(id, out var existing)) return existing;
            var node = new GraphNodeViewModel(id, label, type, x, y);
            _nodeMap[id] = node;
            Nodes.Add(node);
            return node;
        }

        private bool IsAtPoint(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2)) < 5.0;
        }

        private void MergeOverlappingTerminalNodes()
        {
            var terminals = Nodes.Where(n => n.Type == "Terminal").ToList();
            var merged = new HashSet<GraphNodeViewModel>();

            for (int i = 0; i < terminals.Count; i++)
            {
                if (merged.Contains(terminals[i])) continue;

                for (int j = i + 1; j < terminals.Count; j++)
                {
                    if (merged.Contains(terminals[j])) continue;

                    if (IsAtPoint(terminals[i].X, terminals[i].Y, terminals[j].X, terminals[j].Y))
                    {
                        // Redirect edges from j to i
                        var toRemove = terminals[j];
                        var replacement = terminals[i];

                        foreach (var edge in Edges.ToList())
                        {
                            if (edge.FromNode == toRemove) edge.FromNode = replacement;
                            if (edge.ToNode == toRemove) edge.ToNode = replacement;
                        }

                        Nodes.Remove(toRemove);
                        merged.Add(toRemove);
                    }
                }
            }
        }
    }
}
