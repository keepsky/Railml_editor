using System;
using System.Linq;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    public static class RailmlMapper
    {
        public static Railml ToRailml(MainViewModel viewModel, DocumentViewModel doc)
        {
            var railml = new Railml
            {
                Infrastructure = new Infrastructure
                {
                    Id = viewModel.ActiveInfrastructure.Id,
                    Name = viewModel.ActiveInfrastructure.Name,
                    Tracks = new Tracks()
                }
            };
            railml.Namespaces.Add("sehwa", "http://www.sehwa.co.kr/railml");

            // Visualization setup
            var visualizations = new InfrastructureVisualizations();
            var mainVis = new Visualization 
            { 
                Id = "vis1", 
                InfrastructureRef = railml.Infrastructure.Id 
            };
            visualizations.VisualizationList.Add(mainVis);
            railml.InfrastructureVisualizations = visualizations;

            var lineVis = new LineVis();
            mainVis.LineVisList.Add(lineVis);

            var context = new Mappers.MappingContext(viewModel, doc)
            {
                Railml = railml,
                MainLineVis = lineVis,
                MainVis = mainVis
            };

            var trackMapper = new Mappers.TrackMapper();
            var switchMapper = new Mappers.SwitchMapper();
            var routeMapper = new Mappers.RouteMapper();
            var areaMapper = new Mappers.AreaMapper();

            // 1. Create Tracks
            foreach (var element in doc.Elements.OfType<TrackViewModel>())
            {
                var track = new Track();
                trackMapper.MapToRailml(element, track, context);
                railml.Infrastructure.Tracks.TrackList.Add(track);
                context.TrackLookup[element.Id] = element;
            }

            // Serialize TextElements inside mainVis (Generic Labels or other things)
            var textElements = doc.Elements.Where(e => !(e is TrackViewModel || e is SwitchViewModel || e is SignalViewModel || e is TrackCircuitBorderViewModel || e is RouteViewModel || e is AreaViewModel)).ToList();
            if (textElements.Count > 0)
            {
                foreach (var textVm in textElements)
                {
                    mainVis.ObjectVisList.Add(new ObjectVis
                    {
                        Ref = textVm.Id,
                        Position = new VisualizationPosition { X = textVm.X, Y = textVm.Y }
                    });
                }
            }

            // 2. Switches
            foreach (var sw in doc.Elements.OfType<SwitchViewModel>())
            {
                context.SwitchLookup[sw.Id] = sw;
                var switchObj = new Switch();
                switchMapper.MapToRailml(sw, switchObj, context);
            }

            // 4. Create Routes
            var routeList = doc.Elements.OfType<RouteViewModel>().ToList();
            if (routeList.Any())
            {
                railml.Infrastructure.Routes = new Routes();
                foreach (var rVm in routeList)
                {
                    var r = new Route();
                    routeMapper.MapToRailml(rVm, r, context);
                    railml.Infrastructure.Routes.RouteList.Add(r);
                }
            }

            // 5. Create Areas
            var areaList = doc.Elements.OfType<AreaViewModel>().ToList();
            if (areaList.Any())
            {
                railml.Infrastructure.Areas = new Areas();
                foreach (var aVm in areaList)
                {
                    var a = new Area();
                    areaMapper.MapToRailml(aVm, a, context);
                    railml.Infrastructure.Areas.AreaList.Add(a);
                }
            }



            return railml;
        }

        public static System.Collections.Generic.List<BaseElementViewModel> ToViewModelsForSnippet(Railml? railml, MainViewModel viewModel, DocumentViewModel doc)
        {
            var newElements = new System.Collections.Generic.List<BaseElementViewModel>();
            if (railml?.Infrastructure?.Tracks?.TrackList == null) return newElements;

            // Build Coordinate Map from Visualizations
            var coordMap = new System.Collections.Generic.Dictionary<string, VisualizationPosition>();
            if (railml?.InfrastructureVisualizations?.VisualizationList != null)
            {
                foreach (var vis in railml.InfrastructureVisualizations.VisualizationList)
                {
                    foreach (var lVis in vis.LineVisList)
                    {
                        foreach (var tVis in lVis.TrackVisList)
                        {
                            foreach (var eVis in tVis.TrackElementVisList)
                            {
                                if (eVis.Ref != null && eVis.Position != null)
                                {
                                    coordMap[eVis.Ref] = eVis.Position;
                                }
                            }
                        }
                    }

                    if (vis.ObjectVisList != null)
                    {
                        foreach (var oVis in vis.ObjectVisList)
                        {
                            if (oVis.Ref != null && oVis.Position != null)
                            {
                                coordMap[oVis.Ref] = oVis.Position;
                            }
                        }
                    }
                }
            }

            var idMap = new System.Collections.Generic.Dictionary<string, string>();
            
            // Track local counters to avoid duplicates within snippet
            var trackCounter = 0;
            var pointCounter = 0;

            // Helper to get next ID factoring in local counter
            string GetNextSnippetId(string prefix, ref int localCounter)
            {
                int max = 0;
                foreach (var el in doc.Elements)
                {
                    if (el.Id != null && el.Id.StartsWith(prefix) && int.TryParse(el.Id.Substring(prefix.Length), out int num))
                    {
                        if (num > max) max = num;
                    }
                }
                localCounter++;
                return $"{prefix}{max + localCounter}";
            }

            // First pass: Create ID mapping
            foreach (var track in railml.Infrastructure.Tracks.TrackList)
            {
                if (track.Id != null && !idMap.ContainsKey(track.Id))
                {
                    idMap[track.Id] = GetNextSnippetId("tr", ref trackCounter);
                }

                if (track.TrackTopology?.Connections?.Switches != null)
                {
                    foreach (var sw in track.TrackTopology.Connections.Switches)
                    {
                        if (!idMap.ContainsKey(sw.Id))
                        {
                            idMap[sw.Id] = GetNextSnippetId("sw", ref pointCounter);
                        }
                    }
                }
            }

            // Build Connection/Node ID to Track ID Map
            var connToTrackMap = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var track in railml.Infrastructure.Tracks.TrackList)
            {
                if (track.TrackTopology?.TrackBegin != null)
                {
                    connToTrackMap[track.TrackTopology.TrackBegin.Id] = track.Id;
                    if (track.TrackTopology.TrackBegin.ConnectionList != null)
                    {
                        foreach (var conn in track.TrackTopology.TrackBegin.ConnectionList)
                            connToTrackMap[conn.Id] = track.Id;
                    }
                }
                if (track.TrackTopology?.TrackEnd != null)
                {
                    connToTrackMap[track.TrackTopology.TrackEnd.Id] = track.Id;
                    if (track.TrackTopology.TrackEnd.ConnectionList != null)
                    {
                        foreach (var conn in track.TrackTopology.TrackEnd.ConnectionList)
                            connToTrackMap[conn.Id] = track.Id;
                    }
                }
            }

            // Second pass: Create ViewModels
            foreach (var track in railml.Infrastructure.Tracks.TrackList)
            {
                TrackViewModel trackVm;

                // Get coordinates from Visualization Map or falling back
                VisualizationPosition? startPos = null;
                VisualizationPosition? endPos = null;
                VisualizationPosition? midPos = null;

                if (track.TrackTopology?.TrackBegin != null)
                {
                    if (!coordMap.TryGetValue(track.TrackTopology.TrackBegin.Id, out startPos))
                        coordMap.TryGetValue($"{track.Id}_begin", out startPos);
                }

                if (track.TrackTopology?.TrackEnd != null)
                {
                    if (!coordMap.TryGetValue(track.TrackTopology.TrackEnd.Id, out endPos))
                        coordMap.TryGetValue($"{track.Id}_end", out endPos);
                }

                coordMap.TryGetValue($"{track.Id}_mid", out midPos);

                if (track.Code == "corner" && midPos != null)
                {
                    trackVm = new CurvedTrackViewModel { MX = midPos.X, MY = midPos.Y };
                }
                else if (track.Code == "corner" && track.TrackTopology?.CornerPos != null)
                {
                    trackVm = new CurvedTrackViewModel
                    {
                        MX = track.TrackTopology.CornerPos.X,
                        MY = track.TrackTopology.CornerPos.Y
                    };
                }
                else
                {
                    trackVm = new TrackViewModel();
                }

                trackVm.Id = idMap[track.Id];
                trackVm.Name = track.Name;
                trackVm.Description = track.Description;
                trackVm.Type = track.Type;
                trackVm.MainDir = track.MainDir;
                trackVm.Code = track.Code;

                if (startPos != null)
                {
                    trackVm.X = startPos.X;
                    trackVm.Y = startPos.Y;
                }
                else
                {
                    trackVm.X = track.TrackTopology?.TrackBegin?.ScreenPos?.X ?? 0;
                    trackVm.Y = track.TrackTopology?.TrackBegin?.ScreenPos?.Y ?? 0;
                }

                if (endPos != null)
                {
                    trackVm.X2 = endPos.X;
                    trackVm.Y2 = endPos.Y;
                }
                else if (track.TrackTopology?.TrackEnd != null)
                {
                    trackVm.X2 = track.TrackTopology.TrackEnd.ScreenPos?.X ?? 0;
                    trackVm.Y2 = track.TrackTopology.TrackEnd.ScreenPos?.Y ?? 0;
                }
                else
                {
                    trackVm.Length = 100;
                }

                if (track.TrackTopology?.TrackBegin != null)
                {
                    if (track.TrackTopology.TrackBegin.BufferStop != null)
                    {
                        trackVm.BeginType = TrackNodeType.BufferStop;
                        trackVm.BeginNode.Id = track.TrackTopology.TrackBegin.BufferStop.Id;
                    }
                    else if (track.TrackTopology.TrackBegin.OpenEnd != null)
                    {
                        trackVm.BeginType = TrackNodeType.OpenEnd;
                        trackVm.BeginNode.Id = track.TrackTopology.TrackBegin.OpenEnd.Id;
                    }
                    else if (track.TrackTopology.TrackBegin.ConnectionList != null && track.TrackTopology.TrackBegin.ConnectionList.Any())
                    {
                        trackVm.BeginType = TrackNodeType.Connection;
                        trackVm.HasBeginConnection = true;
                        var conn = track.TrackTopology.TrackBegin.ConnectionList.First();
                        if (conn.Ref != null)
                        {
                            string targetRef = conn.Ref.Contains("-") ? conn.Ref.Split('-')[1] : conn.Ref;
                            string targetTrackNum = System.Text.RegularExpressions.Regex.Match(targetRef, @"\d+").Value;
                            string targetTrackId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => System.Text.RegularExpressions.Regex.Match(t.Id, @"\d+").Value == targetTrackNum)?.Id ?? targetRef.Split('_')[0];

                            if (idMap.ContainsKey(targetTrackId)) trackVm.BeginNode.ConnectedTrackId = idMap[targetTrackId];
                            trackVm.BeginNode.ConnectedNodeId = conn.Ref;
                            trackVm.BeginNode.ConnectionId = conn.Id;
                            trackVm.BeginNode.ConnectionRef = conn.Ref;
                        }
                    }
                    trackVm.BeginNode.AbsPos = track.TrackTopology.TrackBegin.AbsPos;
                }

                if (track.TrackTopology?.TrackEnd != null)
                {
                    if (track.TrackTopology.TrackEnd.BufferStop != null)
                    {
                        trackVm.EndType = TrackNodeType.BufferStop;
                        trackVm.EndNode.Id = track.TrackTopology.TrackEnd.BufferStop.Id;
                    }
                    else if (track.TrackTopology.TrackEnd.OpenEnd != null)
                    {
                        trackVm.EndType = TrackNodeType.OpenEnd;
                        trackVm.EndNode.Id = track.TrackTopology.TrackEnd.OpenEnd.Id;
                    }
                    else if (track.TrackTopology.TrackEnd.ConnectionList != null && track.TrackTopology.TrackEnd.ConnectionList.Any())
                    {
                        trackVm.EndType = TrackNodeType.Connection;
                        trackVm.HasEndConnection = true;
                        var conn = track.TrackTopology.TrackEnd.ConnectionList.First();
                        if (conn.Ref != null)
                        {
                            string targetRef = conn.Ref.Contains("-") ? conn.Ref.Split('-')[1] : conn.Ref;
                            string targetTrackNum = System.Text.RegularExpressions.Regex.Match(targetRef, @"\d+").Value;
                            string targetTrackId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => System.Text.RegularExpressions.Regex.Match(t.Id, @"\d+").Value == targetTrackNum)?.Id ?? targetRef.Split('_')[0];

                            if (idMap.ContainsKey(targetTrackId)) trackVm.EndNode.ConnectedTrackId = idMap[targetTrackId];
                            trackVm.EndNode.ConnectedNodeId = conn.Ref;
                            trackVm.EndNode.ConnectionId = conn.Id;
                            trackVm.EndNode.ConnectionRef = conn.Ref;
                        }
                    }
                    trackVm.EndNode.AbsPos = track.TrackTopology.TrackEnd.AbsPos;
                }

                newElements.Add(trackVm);

                // Load Switches
                if (track.TrackTopology?.Connections?.Switches != null)
                {
                    foreach (var sw in track.TrackTopology.Connections.Switches)
                    {
                        // Calculate geometric position on track
                        double pos = sw.Pos;
                        double startX = trackVm.X;
                        double startY = trackVm.Y;
                        double endX = trackVm.X2;
                        double endY = trackVm.Y2;
                        
                        // Default straight line length
                        double length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));

                        // If curved, calculate approximate length and new ratio 
                        bool isCurved = trackVm is CurvedTrackViewModel;
                        CurvedTrackViewModel? ctv = trackVm as CurvedTrackViewModel;
                        
                        if (isCurved && ctv != null)
                        {
                            length = ctv.Length;
                        }

                        if (length < 0.1) length = 1;
                        coordMap.TryGetValue(sw.Id, out var swPos);
                        double swX = swPos?.X ?? 0;
                        double swY = swPos?.Y ?? 0;

                        if (swPos == null)
                        {
                             double ratio = pos / length;
                             
                             if (isCurved && ctv != null)
                             {
                                 // Quadratic Bezier interpolation point for switches on curved tracks
                                 // B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                                 double t = ratio;
                                 double invT = 1.0 - t;
                                 
                                 swX = (invT * invT * startX) + (2 * invT * t * ctv.MX) + (t * t * endX);
                                 swY = (invT * invT * startY) + (2 * invT * t * ctv.MY) + (t * t * endY);
                             }
                             else
                             {
                                 // Standard linear interpolation
                                 swX = startX + ratio * (endX - startX);
                                 swY = startY + ratio * (endY - startY);
                             }
                        }
                        
                        var switchVm = new SwitchViewModel
                        {
                            Id = idMap[sw.Id],
                            Name = sw.AdditionalName?.Name,
                            X = swX,
                            Y = swY,
                            TrackContinueCourse = sw.TrackContinueCourse ?? "straight",
                            NormalPosition = sw.NormalPosition ?? "straight"
                        };

                        // Reconstruction logic
                        var firstConn = sw.ConnectionList.FirstOrDefault();
                        if (firstConn != null)
                        {
                            switchVm.IsScenario1 = (firstConn.Orientation == "outgoing");
                            if (switchVm.IsScenario1)
                            {
                                switchVm.PrincipleTrackId = trackVm.Id;
                                var beginNode = track.TrackTopology?.TrackBegin;
                                var enteringConn = beginNode?.ConnectionList?.FirstOrDefault();
                                if (enteringConn != null)
                                {
                                    string oldRef = enteringConn.Ref;
                                    string oldTrackId = null;
                                    
                                    // Try direct map first (trackId)
                                    if (idMap.ContainsKey(oldRef)) oldTrackId = oldRef;
                                    // Try looking up connection/node ID to Track ID
                                    else if (connToTrackMap.ContainsKey(oldRef)) oldTrackId = connToTrackMap[oldRef];
                                    // Try regex fallback (tr1 from ce1)
                                    else 
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(oldRef, @"\d+");
                                        if (match.Success)
                                        {
                                            var num = match.Value;
                                            oldTrackId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id != null && t.Id.EndsWith(num))?.Id;
                                        }
                                    }

                                    if (oldTrackId != null && idMap.ContainsKey(oldTrackId)) 
                                        switchVm.EnteringTrackId = idMap[oldTrackId];
                                }
                            }
                            else
                            {
                                switchVm.EnteringTrackId = trackVm.Id;
                                var beginNode = track.TrackTopology?.TrackBegin;
                                var principleConn = beginNode?.ConnectionList?.FirstOrDefault();
                                if (principleConn != null)
                                {
                                    string oldRef = principleConn.Ref;
                                    string oldTrackId = null;
                                    
                                    if (idMap.ContainsKey(oldRef)) oldTrackId = oldRef;
                                    else if (connToTrackMap.ContainsKey(oldRef)) oldTrackId = connToTrackMap[oldRef];
                                    else 
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(oldRef, @"\d+");
                                        if (match.Success)
                                        {
                                            var num = match.Value;
                                            oldTrackId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id != null && t.Id.EndsWith(num))?.Id;
                                        }
                                    }

                                    if (oldTrackId != null && idMap.ContainsKey(oldTrackId)) 
                                        switchVm.PrincipleTrackId = idMap[oldTrackId];
                                }
                            }

                            foreach (var c in sw.ConnectionList)
                            {
                                    string oldRef = c.Ref;
                                    string oldTrackId = null;

                                    if (idMap.ContainsKey(oldRef)) oldTrackId = oldRef;
                                    else if (connToTrackMap.ContainsKey(oldRef)) oldTrackId = connToTrackMap[oldRef];
                                    else 
                                    {
                                        var match = System.Text.RegularExpressions.Regex.Match(oldRef, @"\d+");
                                        if (match.Success)
                                        {
                                            var num = match.Value;
                                            oldTrackId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id != null && t.Id.EndsWith(num))?.Id;
                                        }
                                    }

                                    if (oldTrackId != null && idMap.ContainsKey(oldTrackId))
                                    {
                                        var newDivId = idMap[oldTrackId];
                                        switchVm.DivergingTrackIds.Add(newDivId);
#pragma warning disable CS8604, CS8625
                                    switchVm.DivergingConnections.Add(new DivergingConnectionViewModel
                                    {
                                        TrackId = newDivId,
                                        DisplayName = newDivId,
                                        Course = c.Course ?? "straight",
                                        Id = c.Id ?? "",
                                        Ref = c.Ref ?? "",
                                        Orientation = c.Orientation ?? (switchVm.IsScenario1 ? "outgoing" : "incoming")
                                    });
#pragma warning restore CS8604, CS8625
                                }
                            }
                        }
                        newElements.Add(switchVm);
                    }
                }
            }

            // Fix cross-references within snippet
            foreach (var sw in newElements.OfType<SwitchViewModel>())
            {
                foreach (var dc in sw.DivergingConnections)
                {
                    dc.TargetTrack = newElements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == dc.TrackId);
                }
            }

            return newElements;
        }


        public static void LoadIntoViewModel(Railml? railml, MainViewModel viewModel, DocumentViewModel doc)
        {
            doc.Elements.Clear();
            if (railml == null) return;
            if (railml?.Infrastructure != null)
            {
                viewModel.ActiveInfrastructure.Id = railml.Infrastructure.Id ?? "inf001";
                viewModel.ActiveInfrastructure.Name = railml.Infrastructure.Name ?? "Default Infrastructure";
            }
            viewModel.Elements.Clear();

            var context = new Mappers.MappingContext(viewModel, doc) { Railml = railml };
            var trackMapper = new Mappers.TrackMapper();
            var switchMapper = new Mappers.SwitchMapper();
            var routeMapper = new Mappers.RouteMapper();
            var areaMapper = new Mappers.AreaMapper();
            var signalMapper = new Mappers.SignalMapper();

                // Build Coordinate Map from Visualizations
                var coordMap = new System.Collections.Generic.Dictionary<string, VisualizationPosition>();
                if (railml?.InfrastructureVisualizations?.VisualizationList != null)
                {
                    foreach (var vis in railml.InfrastructureVisualizations.VisualizationList)
                    {
                        foreach (var lVis in vis.LineVisList)
                        {
                            foreach (var tVis in lVis.TrackVisList)
                            {
                                foreach (var eVis in tVis.TrackElementVisList)
                                {
                                    if (eVis.Ref != null && eVis.Position != null)
                                    {
                                        coordMap[eVis.Ref] = eVis.Position;
                                    }
                                }
                            }
                        }

                        // Load Object Visualizations (Switches, etc.)
                        if (vis.ObjectVisList != null)
                        {
                            foreach (var oVis in vis.ObjectVisList)
                            {
                                if (oVis.Ref != null && oVis.Position != null)
                                {
                                    coordMap[oVis.Ref] = oVis.Position;
                                }
                            }
                        }
                    }
                }
                if (railml?.Infrastructure?.Tracks?.TrackList != null)
                {
                    foreach (var track in railml.Infrastructure.Tracks.TrackList)
                    {
                        TrackViewModel trackVm;
                        
                        // Try to get coordinates from Visualization Map first
                        VisualizationPosition? startPos = null;
                        VisualizationPosition? endPos = null;
                        VisualizationPosition? midPos = null;

                        if (track.TrackTopology?.TrackBegin != null)
                        {
                            if (!coordMap.TryGetValue(track.TrackTopology.TrackBegin.Id, out startPos))
                                coordMap.TryGetValue($"{track.Id}_begin", out startPos);
                        }

                        if (track.TrackTopology?.TrackEnd != null)
                        {
                            if (!coordMap.TryGetValue(track.TrackTopology.TrackEnd.Id, out endPos))
                                coordMap.TryGetValue($"{track.Id}_end", out endPos);
                        }

                        coordMap.TryGetValue($"{track.Id}_mid", out midPos);

                        if (track.Code == "corner" && midPos != null)
                        {
                            trackVm = new CurvedTrackViewModel { MX = midPos.X, MY = midPos.Y };
                        }
                        else if (track.Code == "corner" && track.TrackTopology?.CornerPos != null)
                        {
                            trackVm = new CurvedTrackViewModel
                            {
                                MX = track.TrackTopology.CornerPos.X,
                                MY = track.TrackTopology.CornerPos.Y
                            };
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

                        trackMapper.MapToViewModel(track, trackVm, context);

                        // Load BufferStop/OpenEnd
                        if (track.TrackTopology?.TrackBegin != null)
                        {
                            trackVm.BeginNode.AbsPos = track.TrackTopology.TrackBegin.AbsPos;
                            if (track.TrackTopology.TrackBegin.BufferStop != null)
                            {
                                trackVm.BeginType = TrackNodeType.BufferStop;
                                trackVm.BeginNode.Id = track.TrackTopology.TrackBegin.BufferStop.Id;
                                trackVm.BeginNode.Code = track.TrackTopology.TrackBegin.BufferStop.Code;
                                trackVm.BeginNode.Name = track.TrackTopology.TrackBegin.BufferStop.Name;
                                trackVm.BeginNode.Description = track.TrackTopology.TrackBegin.BufferStop.Description;
                            }
                            else if (track.TrackTopology.TrackBegin.OpenEnd != null)
                            {
                                trackVm.BeginType = TrackNodeType.OpenEnd;
                                trackVm.BeginNode.Id = track.TrackTopology.TrackBegin.OpenEnd.Id;
                                trackVm.BeginNode.Code = track.TrackTopology.TrackBegin.OpenEnd.Code;
                                trackVm.BeginNode.Name = track.TrackTopology.TrackBegin.OpenEnd.Name;
                                trackVm.BeginNode.Description = track.TrackTopology.TrackBegin.OpenEnd.Description;
                            }
                            else if (track.TrackTopology.TrackBegin.ConnectionList != null && track.TrackTopology.TrackBegin.ConnectionList.Any())
                            {
                                trackVm.BeginType = TrackNodeType.Connection;
                                var conn = track.TrackTopology.TrackBegin.ConnectionList.First();
                                    if (conn.Ref != null)
                                    {
                                        string targetRef = conn.Ref.Contains("-") ? conn.Ref.Split('-')[1] : conn.Ref;
                                        string trackNum = System.Text.RegularExpressions.Regex.Match(targetRef, @"\d+").Value;
                                        trackVm.BeginNode.ConnectedTrackId = "tr" + trackNum;
                                        trackVm.BeginNode.ConnectedNodeId = conn.Ref;
                                        trackVm.BeginNode.ConnectionId = conn.Id;
                                        trackVm.BeginNode.ConnectionRef = conn.Ref;
                                    }
                            }
                            else
                            {
                                trackVm.BeginType = TrackNodeType.None;
                            }

                            // Check connection
                            if (track.TrackTopology.TrackBegin.ConnectionList != null && track.TrackTopology.TrackBegin.ConnectionList.Any())
                            {
                                trackVm.HasBeginConnection = true;
                            }
                        }

                        if (track.TrackTopology?.TrackEnd != null)
                        {
                            trackVm.EndNode.AbsPos = track.TrackTopology.TrackEnd.AbsPos;
                            if (track.TrackTopology.TrackEnd.BufferStop != null)
                            {
                                trackVm.EndType = TrackNodeType.BufferStop;
                                trackVm.EndNode.Id = track.TrackTopology.TrackEnd.BufferStop.Id;
                                trackVm.EndNode.Code = track.TrackTopology.TrackEnd.BufferStop.Code;
                                trackVm.EndNode.Name = track.TrackTopology.TrackEnd.BufferStop.Name;
                                trackVm.EndNode.Description = track.TrackTopology.TrackEnd.BufferStop.Description;
                            }
                            else if (track.TrackTopology.TrackEnd.OpenEnd != null)
                            {
                                trackVm.EndType = TrackNodeType.OpenEnd;
                                trackVm.EndNode.Id = track.TrackTopology.TrackEnd.OpenEnd.Id;
                                trackVm.EndNode.Code = track.TrackTopology.TrackEnd.OpenEnd.Code;
                                trackVm.EndNode.Name = track.TrackTopology.TrackEnd.OpenEnd.Name;
                                trackVm.EndNode.Description = track.TrackTopology.TrackEnd.OpenEnd.Description;
                            }
                            else if (track.TrackTopology.TrackEnd.ConnectionList != null && track.TrackTopology.TrackEnd.ConnectionList.Any())
                            {
                                trackVm.EndType = TrackNodeType.Connection;
                                var conn = track.TrackTopology.TrackEnd.ConnectionList.First();
                                    if (conn.Ref != null)
                                    {
                                        string targetRef = conn.Ref.Contains("-") ? conn.Ref.Split('-')[1] : conn.Ref;
                                        string trackNum = System.Text.RegularExpressions.Regex.Match(targetRef, @"\d+").Value;
                                        trackVm.EndNode.ConnectedTrackId = "tr" + trackNum;
                                        trackVm.EndNode.ConnectedNodeId = conn.Ref;
                                        trackVm.EndNode.ConnectionId = conn.Id;
                                        trackVm.EndNode.ConnectionRef = conn.Ref;
                                    }
                            }
                            else
                            {
                                trackVm.EndType = TrackNodeType.None;
                            }

                            // Check connection
                            if (track.TrackTopology.TrackEnd.ConnectionList != null && track.TrackTopology.TrackEnd.ConnectionList.Any())
                            {
                                trackVm.HasEndConnection = true;
                            }
                        }
                        trackVm.Code = track.Code;

                        // Set X, Y
                        if (startPos != null)
                        {
                            trackVm.X = startPos.X;
                            trackVm.Y = startPos.Y;
                        }
                        else
                        {
                            trackVm.X = track.TrackTopology?.TrackBegin?.ScreenPos?.X ?? 0;
                            trackVm.Y = track.TrackTopology?.TrackBegin?.ScreenPos?.Y ?? 0;
                        }
                        
                        // Set X2, Y2
                        if (endPos != null)
                        {
                            trackVm.X2 = endPos.X;
                            trackVm.Y2 = endPos.Y;
                        }
                        else if (track.TrackTopology?.TrackEnd != null)
                        {
                            trackVm.X2 = track.TrackTopology.TrackEnd.ScreenPos?.X ?? 0;
                            trackVm.Y2 = track.TrackTopology.TrackEnd.ScreenPos?.Y ?? 0;
                        }
                        else
                        {
                            // Default length 100 if no end specified
                            trackVm.Length = 100; 
                        }
                        doc.Elements.Add(trackVm);
                        

                        // NEW: Load Switches from TrackTopology.Connections (Standardized Location)
                        if (track.TrackTopology?.Connections?.Switches != null)
                        {
                            foreach (var sw in track.TrackTopology.Connections.Switches)
                            {                                var switchVm = doc.Elements.OfType<SwitchViewModel>().FirstOrDefault(e => e.Id == sw.Id);
                                VisualizationPosition? swPos = null;
                                coordMap.TryGetValue(sw.Id, out swPos);

                                if (switchVm == null)
                                {
                                    if (swPos != null)
                                    {
                                        switchVm = new SwitchViewModel
                                        {
                                            Id = sw.Id,
                                            Name = sw.AdditionalName?.Name,
                                             X = swPos.X,
                                             Y = swPos.Y,
                                            TrackContinueCourse = sw.TrackContinueCourse ?? "straight",
                                            NormalPosition = sw.NormalPosition ?? "straight"
                                        };
                                    }
                                    else
                                    {
                                        // Calculate Position (Legacy fallback)
                                        double pos = sw.Pos;
                                        double startX = trackVm.X;
                                        double startY = trackVm.Y;
                                        double endX = trackVm.X2;
                                        double endY = trackVm.Y2;
                                        double length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                                        if (length < 0.1) length = 1;
                                        double ratio = pos / length;
                                        double calcX = startX + ratio * (endX - startX);
                                        double calcY = startY + ratio * (endY - startY);

                                        switchVm = new SwitchViewModel
                                        {
                                            Id = sw.Id,
                                            Name = sw.AdditionalName?.Name,
                                             X = calcX,
                                             Y = calcY,
                                            TrackContinueCourse = sw.TrackContinueCourse ?? "straight",
                                            NormalPosition = sw.NormalPosition ?? "straight"
                                        };
                                    }                                    doc.Elements.Add(switchVm);
                                }
                                else if (swPos != null)
                                {
                                    // Update existing switch position if found in visualization map
                                    switchVm.X = swPos.X;
                                    switchVm.Y = swPos.Y;
                                }

                                    // Topological reconstruction
                                var firstConn = sw.ConnectionList.FirstOrDefault();
                                if (firstConn != null)
                                {
                                    switchVm.IsScenario1 = (firstConn.Orientation == "outgoing");
                                    if (switchVm.IsScenario1)
                                    {
                                        switchVm.PrincipleTrackId = track.Id;
                                        // Entering track is the one connected to principle track begin
                                        var beginNode = track.TrackTopology?.TrackBegin;
                                        if (beginNode?.ConnectionList != null)
                                        {
                                            var enteringConn = beginNode.ConnectionList.FirstOrDefault();
                                            if (enteringConn != null) 
                                            {
                                                string trackNum = System.Text.RegularExpressions.Regex.Match(enteringConn.Ref, @"\d+").Value;
                                                switchVm.EnteringTrackId = "tr" + trackNum;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        switchVm.EnteringTrackId = track.Id;
                                        // Principle track is the one connected to entering track begin
                                        var beginNode = track.TrackTopology?.TrackBegin;
                                        if (beginNode?.ConnectionList != null)
                                        {
                                            var principleConn = beginNode.ConnectionList.FirstOrDefault();
                                            if (principleConn != null)
                                            {
                                                string trackNum = System.Text.RegularExpressions.Regex.Match(principleConn.Ref, @"\d+").Value;
                                                switchVm.PrincipleTrackId = "tr" + trackNum;
                                            }
                                        }
                                    }

                                    foreach (var c in sw.ConnectionList)
                                    {
                                        string divTrackNum = System.Text.RegularExpressions.Regex.Match(c.Ref, @"\d+").Value;
                                        var divId = "tr" + divTrackNum;
                                        switchVm.DivergingTrackIds.Add(divId);
                                        switchVm.DivergingConnections.Add(new DivergingConnectionViewModel
                                        {
                                            TrackId = divId,
                                            DisplayName = divId, // Re-load name later if needed
                                            Course = c.Course ?? "straight",
                                            Id = c.Id,
                                            Ref = c.Ref,
                                            Orientation = c.Orientation ?? (switchVm.IsScenario1 ? "outgoing" : "incoming")
                                        });
                                    }
                                }
                            }
                        }

                        // Load Signals
                        if (track.OcsElements?.Signals?.SignalList != null)
                        {
                            foreach (var signal in track.OcsElements.Signals.SignalList)
                            {
                                // Try to get coordinates from Visualization Map first
                                VisualizationPosition? sigPos = null;
                                coordMap.TryGetValue(signal.Id, out sigPos);

                                var signalVm = new SignalViewModel
                                {
                                    Id = signal.Id,
                                    Direction = signal.Dir ?? "up",
                                    Type = signal.Type,
                                    Function = signal.Function,
                                    Name = signal.AdditionalName?.Name,
                                    X = sigPos?.X ?? signal.ScreenPos?.X ?? signal.X,
                                    Y = sigPos?.Y ?? signal.ScreenPos?.Y ?? signal.Y,
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
                                doc.Elements.Add(signalVm);
                            }
                        }

                        // Load Borders
                        if (track.OcsElements?.TrainDetectionElements?.TrackCircuitBorderList != null)
                        {
                            foreach (var border in track.OcsElements.TrainDetectionElements.TrackCircuitBorderList)
                            {
                                VisualizationPosition? borderPos = null;
                                coordMap.TryGetValue(border.Id, out borderPos);

                                var borderVm = new TrackCircuitBorderViewModel
                                {
                                    Id = border.Id,
                                    Name = border.Name,
                                    Code = border.Code,
                                    Description = border.Description,
                                    Pos = border.Pos,
                                    X = borderPos?.X ?? 0,
                                    Y = borderPos?.Y ?? 0,
                                    Angle = Math.Atan2(trackVm.Y2 - trackVm.Y, trackVm.X2 - trackVm.X) * 180 / Math.PI,
                                    RelatedTrackId = track.Id
                                };
                                doc.Elements.Add(borderVm);
                            }
                        }
                    }
                }

                if (railml?.Infrastructure?.Routes?.RouteList != null)
                {
                    foreach (var r in railml.Infrastructure.Routes.RouteList)
                    {
                        var rVm = new RouteViewModel();
                        routeMapper.MapToViewModel(r, rVm, context);
                        doc.Elements.Add(rVm);
                    }
                }

                // Load Areas
                if (railml?.Infrastructure?.Areas?.AreaList != null)
                {
                    foreach (var aObj in railml.Infrastructure.Areas.AreaList)
                    {
                        var aVm = new AreaViewModel();
                        areaMapper.MapToViewModel(aObj, aVm, context);
                        doc.Elements.Add(aVm);
                    }
                }

            // Post-process to update DisplayNames in DivergingConnections            
            foreach (var sw in doc.Elements.OfType<SwitchViewModel>())
            {
                foreach (var dc in sw.DivergingConnections)
                {                    var track = doc.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == dc.TrackId);
                    if (track != null)
                    {
                        dc.TargetTrack = track;
                    }
                }
            }
        }
    public static string GetRailmlId(string? id)
        {
            if (string.IsNullOrEmpty(id)) return id ?? "";
            if (id.StartsWith("PT") || id.StartsWith("T")) 
            {
                var match = System.Text.RegularExpressions.Regex.Match(id, @"\d+");
                if (match.Success) return "tr" + match.Value;
                return "tr" + id;
            }
            if (id.StartsWith("P") && !id.StartsWith("PT"))
            {
                 var match = System.Text.RegularExpressions.Regex.Match(id, @"\d+");
                 if (match.Success) return "sw" + match.Value;
                 return "sw" + id;
            }
            return id;
        }
    }
}