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

            // 1. Create Tracks
            var trackMap = new System.Collections.Generic.Dictionary<string, Track>();
            foreach (var element in viewModel.Elements.OfType<TrackViewModel>())
            {
                // Determine if Curved
                bool isCurved = element is CurvedTrackViewModel;
                CurvedTrackViewModel ctv = isCurved ? (CurvedTrackViewModel)element : null;

                var trackId = GetRailmlId(element.Id);
                var track = new Track
                {
                    Id = trackId,
                    Name = element.Name,
                    Description = element.Description,
                    Type = element.Type,
                    MainDir = element.MainDir,
                    Code = element.Code ?? (isCurved ? "corner" : "plain"),
                    TrackTopology = new TrackTopology
                    {
                        TrackBegin = new TrackNode 
                        { 
                            Id = "tb" + System.Text.RegularExpressions.Regex.Match(trackId, @"\d+").Value,
                            Pos = 0,
                            ScreenPos = new ScreenPos { X = element.X, Y = element.Y }
                        },
                        TrackEnd = new TrackNode 
                        { 
                            Id = "te" + System.Text.RegularExpressions.Regex.Match(trackId, @"\d+").Value,
                            Pos = (int)element.Length,
                            ScreenPos = new ScreenPos { X = element.X2, Y = element.Y2 }
                        }
                    },
                };

                // Visualization for Track
                var trackVis = new TrackVis { Ref = trackId };
                lineVis.TrackVisList.Add(trackVis);

                // Track Begin Vis
                trackVis.TrackElementVisList.Add(new TrackElementVis
                {
                    Ref = track.TrackTopology.TrackBegin.Id,
                    Position = new VisualizationPosition { X = element.X, Y = element.Y }
                });

                // Track End Vis
                trackVis.TrackElementVisList.Add(new TrackElementVis
                {
                    Ref = track.TrackTopology.TrackEnd.Id,
                    Position = new VisualizationPosition { X = element.X2, Y = element.Y2 }
                });

                // Curved Midpoint Vis
                if (isCurved)
                {
                    trackVis.TrackElementVisList.Add(new TrackElementVis
                    {
                        Ref = $"{trackId}_mid", // Midpoint/Corner coordinate
                        Position = new VisualizationPosition { X = ctv.MX, Y = ctv.MY }
                    });
                }

                // Add Signals and Borders bound to this track
                var trackVm = element as TrackViewModel; // Cast to access properties

                // Map BufferStop/OpenEnd from ViewModel
                if (trackVm != null)
                {
                    if (trackVm.BeginType == TrackNodeType.BufferStop)
                    {
                        track.TrackTopology.TrackBegin.BufferStop = new BufferStop { Id = trackVm.BeginNode.Id, Code = trackVm.BeginNode.Code, Name = trackVm.BeginNode.Name, Description = trackVm.BeginNode.Description };
                    }
                    else if (trackVm.BeginType == TrackNodeType.OpenEnd)
                    {
                        track.TrackTopology.TrackBegin.OpenEnd = new OpenEnd { Id = trackVm.BeginNode.Id, Code = trackVm.BeginNode.Code, Name = trackVm.BeginNode.Name, Description = trackVm.BeginNode.Description };
                    }

                    if (trackVm.EndType == TrackNodeType.BufferStop)
                    {
                        track.TrackTopology.TrackEnd.BufferStop = new BufferStop { Id = trackVm.EndNode.Id, Code = trackVm.EndNode.Code, Name = trackVm.EndNode.Name, Description = trackVm.EndNode.Description };
                    }
                    else if (trackVm.EndType == TrackNodeType.OpenEnd)
                    {
                        track.TrackTopology.TrackEnd.OpenEnd = new OpenEnd { Id = trackVm.EndNode.Id, Code = trackVm.EndNode.Code, Name = trackVm.EndNode.Name, Description = trackVm.EndNode.Description };
                    }
                }
                var boundSignals = viewModel.Elements.OfType<SignalViewModel>().Where(s => s.RelatedTrackId == element.Id).ToList();
                var boundBorders = viewModel.Elements.OfType<TrackCircuitBorderViewModel>().Where(b => b.RelatedTrackId == element.Id).ToList();
                
                if (boundSignals.Count > 0 || boundBorders.Count > 0)
                {
                    track.OcsElements = new OcsElements();
                    
                    if (boundSignals.Count > 0)
                    {
                        track.OcsElements.Signals = new Signals();
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
                                ScreenPos = new ScreenPos { X = sigVm.X, Y = sigVm.Y }
                            };
                            track.OcsElements.Signals.SignalList.Add(signal);

                            // Signal Vis
                            trackVis.TrackElementVisList.Add(new TrackElementVis
                            {
                                Ref = signal.Id,
                                Position = new VisualizationPosition { X = sigVm.X, Y = sigVm.Y }
                            });
                        }
                    }

                    if (boundBorders.Count > 0)
                    {
                        track.OcsElements.TrainDetectionElements = new TrainDetectionElements();
                        foreach (var borderVm in boundBorders)
                        {
                            var border = new TrackCircuitBorder
                            {
                                Id = borderVm.Id,
                                Name = borderVm.Name,
                                Code = borderVm.Code,
                                Description = borderVm.Description,
                                Pos = (int)borderVm.Pos
                            };
                            track.OcsElements.TrainDetectionElements.TrackCircuitBorderList.Add(border);

                            // Border Vis
                            trackVis.TrackElementVisList.Add(new TrackElementVis
                            {
                                Ref = border.Id,
                                Position = new VisualizationPosition { X = borderVm.X, Y = borderVm.Y }
                            });
                        }
                    }
                }

                railml.Infrastructure.Tracks.TrackList.Add(track);
                trackMap[element.Id] = track;
            }

            // 2. Add Switches (they are nested in Tracks/TrackTopology/Connections)
            // Need to handle switch visualizations after they are created in the next loop or integrated
            // Actually, existing code generates connections/switches by analyzing overlaps.
            // Let's keep that logic but extract visualization data.
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

                    double ax = nodeA.ScreenPos?.X ?? 0;
                    double ay = nodeA.ScreenPos?.Y ?? 0;
                    double bx = nodeB.ScreenPos?.X ?? 0;
                    double by = nodeB.ScreenPos?.Y ?? 0;

                    double dist = Math.Sqrt(Math.Pow(ax - bx, 2) + Math.Pow(ay - by, 2));
                    if(dist < 5.0) overlappingNodes.Add(nodeB);
                }

                if (overlappingNodes.Count > 0)
                {
                    var parentTrack = nodeToTrack[nodeA];
                    bool isBeginA = nodeA.Id.StartsWith("tb"); // Updated to use new prefix
                    if (overlappingNodes.Count == 1)
                    {
                         foreach (var nodeB in overlappingNodes)
                         {
                             bool isBeginB = nodeB.Id.StartsWith("tb");
                             if (isBeginA == isBeginB) continue; 

                             var targetTrack = nodeToTrack[nodeB];
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
                    }
                    else
                    {
                         // Switch Connection
                         var nodeCoordX = nodeA.ScreenPos?.X ?? 0;
                         var nodeCoordY = nodeA.ScreenPos?.Y ?? 0;
                         var swVm = viewModel.Elements.OfType<SwitchViewModel>()
                                 .FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - nodeCoordX, 2) + Math.Pow(s.Y - nodeCoordY, 2)) < 500.0);
                         
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
                                 if (parentTrack.TrackTopology.Connections == null) 
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
                                             Orientation = swVm.IsScenario1 ? "outgoing" : "incoming",
                                             Course = connVm?.Course ?? "straight"
                                         });

                                         // Add reciprocal connection to diverging track node
                                         var divTrackObj = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => t.Id == GetRailmlId(divId));
                                         if (divTrackObj != null)
                                         {
                                             var targetNode = swVm.IsScenario1 ? divTrackObj.TrackTopology.TrackBegin : divTrackObj.TrackTopology.TrackEnd;
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


            // 4. Create Routes
            var routeList = viewModel.Elements.OfType<RouteViewModel>().ToList();
            if (routeList.Any())
            {
                railml.Infrastructure.Routes = new Routes();
                foreach (var rVm in routeList)
                {
                    var r = new Route
                    {
                        Id = rVm.Id,
                        Name = rVm.Name,
                        Code = rVm.Code,
                        Description = rVm.Description,
                        ApproachPointRef = rVm.ApproachPointRef,
                        EntryRef = rVm.EntryRef,
                        ExitRef = rVm.ExitRef,
                        OverlapEndRef = rVm.OverlapEndRef,
                        ProceedSpeed = rVm.ProceedSpeed,
                        ReleaseTriggerHead = rVm.ReleaseTriggerHead,
                        ReleaseTriggerHeadSpecified = true,
                        ReleaseTriggerRef = rVm.ReleaseTriggerRef
                    };

                    foreach (var sV in rVm.SwitchAndPositions)
                    {
                        r.SwitchAndPositionList.Add(new SwitchAndPosition
                        {
                            SwitchRef = sV.SwitchRef,
                            SwitchPosition = sV.SwitchPosition
                        });
                    }

                    foreach (var sV in rVm.OverlapSwitchAndPositions)
                    {
                        r.OverlapSwitchAndPositionList.Add(new SwitchAndPosition
                        {
                            SwitchRef = sV.SwitchRef,
                            SwitchPosition = sV.SwitchPosition
                        });
                    }

                    if (rVm.ReleaseSections.Any())
                    {
                        r.ReleaseGroup = new ReleaseGroup();
                        foreach (var rsV in rVm.ReleaseSections)
                        {
                            r.ReleaseGroup.TrackSectionRefList.Add(new TrackSectionRef
                            {
                                Ref = rsV.TrackRef,
                                FlankProtection = rsV.FlankProtection
                            });
                        }
                    }

                    railml.Infrastructure.Routes.RouteList.Add(r);
                }
            }

            // 5. Create Areas
            var areaList = viewModel.Elements.OfType<AreaViewModel>().ToList();
            if (areaList.Any())
            {
                railml.Infrastructure.Areas = new Areas();
                foreach (var aVm in areaList)
                {
                    var a = new Area
                    {
                        Id = aVm.Id,
                        Name = aVm.Name,
                        Description = aVm.Description,
                        Type = aVm.Type
                    };
                    foreach (var b in aVm.Borders)
                    {
                        a.IsLimitedByList.Add(new IsLimitedBy { Ref = b.Id });
                    }
                    railml.Infrastructure.Areas.AreaList.Add(a);
                }
            }

            // 6. Save Switch Visualizations (Point Coordinates)
            foreach (var swVm in viewModel.Elements.OfType<SwitchViewModel>())
            {
                var objVis = new ObjectVis
                {
                    Ref = swVm.Id,
                    Position = new VisualizationPosition { X = swVm.X, Y = swVm.Y }
                };
                mainVis.ObjectVisList.Add(objVis);
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


        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippet(string path, MainViewModel viewModel)
        {
            var newElements = new System.Collections.Generic.List<BaseElementViewModel>();
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            
            Railml? railml = null;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    railml = (Railml?)serializer.Deserialize(fs);
                }
            }
            catch
            {
                return newElements;
            }

            return ConvertRailmlToViewModels(railml, viewModel);
        }

        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippetFromXml(string xmlContent, MainViewModel viewModel)
        {
            var newElements = new System.Collections.Generic.List<BaseElementViewModel>();
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            
            Railml? railml = null;
            try
            {
                using (StringReader sr = new StringReader(xmlContent))
                {
                    railml = (Railml?)serializer.Deserialize(sr);
                }
            }
            catch
            {
                // Return empty or throw? Empty for safety.
                return newElements;
            }

            return ConvertRailmlToViewModels(railml, viewModel);
        }

        private System.Collections.Generic.List<BaseElementViewModel> ConvertRailmlToViewModels(Railml? railml, MainViewModel viewModel)
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
                foreach (var el in viewModel.Elements)
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
                if (!idMap.ContainsKey(track.Id))
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
                    if (!coordMap.TryGetValue(track.TrackTopology.TrackEnd.Id, out startPos))
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
                        }
                    }
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
                        }
                    }
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
                        double length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                        if (length < 0.1) length = 1;
                        coordMap.TryGetValue(sw.Id, out var swPos);
                        double swX = swPos?.X ?? 0;
                        double swY = swPos?.Y ?? 0;

                        if (swPos == null)
                        {
                             double ratio = pos / length;
                             swX = startX + ratio * (endX - startX);
                             swY = startY + ratio * (endY - startY);
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
                                    var oldRef = enteringConn.Ref.Split('_')[0];
                                    if (idMap.ContainsKey(oldRef)) switchVm.EnteringTrackId = idMap[oldRef];
                                }
                            }
                            else
                            {
                                switchVm.EnteringTrackId = trackVm.Id;
                                var beginNode = track.TrackTopology?.TrackBegin;
                                var principleConn = beginNode?.ConnectionList?.FirstOrDefault();
                                if (principleConn != null)
                                {
                                    var oldRef = principleConn.Ref.Split('_')[0];
                                    if (idMap.ContainsKey(oldRef)) switchVm.PrincipleTrackId = idMap[oldRef];
                                }
                            }

                            foreach (var c in sw.ConnectionList)
                            {
                                    string oldDivNum = System.Text.RegularExpressions.Regex.Match(c.Ref, @"\d+").Value;
                                    string oldDivId = railml.Infrastructure.Tracks.TrackList.FirstOrDefault(t => System.Text.RegularExpressions.Regex.Match(t.Id, @"\d+").Value == oldDivNum)?.Id ?? c.Ref.Split('_')[0];

                                    if (idMap.ContainsKey(oldDivId))
                                    {
                                        var newDivId = idMap[oldDivId];
                                        switchVm.DivergingTrackIds.Add(newDivId);
                                    switchVm.DivergingConnections.Add(new DivergingConnectionViewModel
                                    {
                                        TrackId = newDivId,
                                        DisplayName = newDivId,
                                        Course = c.Course ?? "straight"
                                    });
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

        public void Load(string path, MainViewModel viewModel)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                var railml = (Railml?)serializer.Deserialize(fs);
 
                if (railml?.Infrastructure != null)
                {
                    viewModel.ActiveInfrastructure.Id = railml.Infrastructure.Id ?? "inf001";
                    viewModel.ActiveInfrastructure.Name = railml.Infrastructure.Name ?? "Default Infrastructure";
                }
                
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

                viewModel.Elements.Clear();

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

                        trackVm.Id = track.Id;
                        trackVm.Name = track.Name;
                        trackVm.Description = track.Description;
                        trackVm.Type = track.Type;
                        trackVm.MainDir = track.MainDir;
                        trackVm.Code = track.Code;

                        // Load BufferStop/OpenEnd
                        if (track.TrackTopology?.TrackBegin != null)
                        {
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

                        viewModel.Elements.Add(trackVm);
                        

                        // NEW: Load Switches from TrackTopology.Connections (Standardized Location)
                        if (track.TrackTopology?.Connections?.Switches != null)
                        {
                            foreach (var sw in track.TrackTopology.Connections.Switches)
                            {
                                var switchVm = viewModel.Elements.OfType<SwitchViewModel>().FirstOrDefault(e => e.Id == sw.Id);
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
                                    }
                                    viewModel.Elements.Add(switchVm);
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
                                                switchVm.EnteringTrackId = enteringConn.Ref.Split('_')[0]; // Simple recovery
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
                                                switchVm.PrincipleTrackId = principleConn.Ref.Split('_')[0];
                                            }
                                        }
                                    }

                                    foreach (var c in sw.ConnectionList)
                                    {
                                        var divId = c.Ref.Split('_')[0];
                                        switchVm.DivergingTrackIds.Add(divId);
                                        switchVm.DivergingConnections.Add(new DivergingConnectionViewModel
                                        {
                                            TrackId = divId,
                                            DisplayName = divId, // Re-load name later if needed
                                            Course = c.Course ?? "straight"
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

                                viewModel.Elements.Add(signalVm);
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

                                viewModel.Elements.Add(borderVm);
                            }
                        }
                    }
                }

                if (railml?.Infrastructure?.Routes?.RouteList != null)
                {
                    foreach (var r in railml.Infrastructure.Routes.RouteList)
                    {
                        var rVm = new RouteViewModel
                        {
                            Id = r.Id,
                            Name = r.Name,
                            Code = r.Code,
                            Description = r.Description,
                            ApproachPointRef = r.ApproachPointRef,
                            EntryRef = r.EntryRef,
                            ExitRef = r.ExitRef,
                            OverlapEndRef = r.OverlapEndRef,
                            ProceedSpeed = r.ProceedSpeed ?? "R",
                            ReleaseTriggerHead = r.ReleaseTriggerHead,
                            ReleaseTriggerRef = r.ReleaseTriggerRef
                        };

                        if (r.SwitchAndPositionList != null)
                        {
                            foreach (var s in r.SwitchAndPositionList)
                            {
                                rVm.SwitchAndPositions.Add(new SwitchPositionViewModel
                                {
                                    SwitchRef = s.SwitchRef,
                                    SwitchPosition = s.SwitchPosition,
                                    RemoveCommand = new RelayCommand(p => rVm.SwitchAndPositions.Remove(p as SwitchPositionViewModel))
                                });
                            }
                        }

                        if (r.OverlapSwitchAndPositionList != null)
                        {
                            foreach (var s in r.OverlapSwitchAndPositionList)
                            {
                                rVm.OverlapSwitchAndPositions.Add(new SwitchPositionViewModel
                                {
                                    SwitchRef = s.SwitchRef,
                                    SwitchPosition = s.SwitchPosition,
                                    RemoveCommand = new RelayCommand(p => rVm.OverlapSwitchAndPositions.Remove(p as SwitchPositionViewModel))
                                });
                            }
                        }

                        if (r.ReleaseGroup?.TrackSectionRefList != null)
                        {
                            foreach (var rs in r.ReleaseGroup.TrackSectionRefList)
                            {
                                rVm.ReleaseSections.Add(new ReleaseSectionViewModel
                                {
                                    TrackRef = rs.Ref,
                                    FlankProtection = rs.FlankProtection,
                                    RemoveCommand = new RelayCommand(p => rVm.ReleaseSections.Remove(p as ReleaseSectionViewModel))
                                });
                            }
                        }

                        viewModel.Elements.Add(rVm);
                    }
                }

                // Load Areas
                if (railml?.Infrastructure?.Areas?.AreaList != null)
                {
                    foreach (var aObj in railml.Infrastructure.Areas.AreaList)
                    {
                        var aVm = new AreaViewModel
                        {
                            Id = aObj.Id,
                            Name = aObj.Name,
                            Description = aObj.Description,
                            Type = aObj.Type
                        };
                        foreach (var lim in aObj.IsLimitedByList)
                        {
                            var border = viewModel.Elements.OfType<TrackCircuitBorderViewModel>().FirstOrDefault(b => b.Id == lim.Ref);
                            if (border != null)
                            {
                                aVm.Borders.Add(border);
                            }
                        }
                        viewModel.Elements.Add(aVm);
                    }
                }
            }

            // Post-process to update DisplayNames in DivergingConnections
            foreach (var sw in viewModel.Elements.OfType<SwitchViewModel>())
            {
                foreach (var dc in sw.DivergingConnections)
                {
                    var track = viewModel.Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == dc.TrackId);
                    if (track != null)
                    {
                        dc.TargetTrack = track;
                    }
                }
            }
        }
        private string GetRailmlId(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
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
