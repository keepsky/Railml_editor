using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using System;
using System.Linq;

namespace RailmlEditor.Services.Mappers
{
    public class TrackMapper : IRailmlElementMapper<TrackViewModel, Track>
    {
        public void MapToRailml(TrackViewModel source, Track destination, MappingContext context)
        {
            // Determine if Curved
            bool isCurved = source is CurvedTrackViewModel;
            CurvedTrackViewModel? ctv = isCurved ? (CurvedTrackViewModel)source : null;

            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Type = source.Type;
            destination.MainDir = source.MainDir;
            destination.Code = source.Code ?? (isCurved ? "corner" : "plain");
            destination.TrackTopology = new TrackTopology
            {
                TrackBegin = new TrackNode
                {
                    Id = "tb" + System.Text.RegularExpressions.Regex.Match(source.Id ?? "", @"\d+").Value,
                    Pos = 0,
                    AbsPos = source.BeginNode.AbsPos,
                    ScreenPos = new ScreenPos { X = source.X, Y = source.Y }
                },
                TrackEnd = new TrackNode
                {
                    Id = "te" + System.Text.RegularExpressions.Regex.Match(source.Id ?? "", @"\d+").Value,
                    Pos = (int)source.Length,
                    AbsPos = source.EndNode.AbsPos,
                    ScreenPos = new ScreenPos { X = source.X2, Y = source.Y2 }
                }
            };

            // Map BufferStop/OpenEnd
            if (source.BeginType == TrackNodeType.BufferStop)
            {
                destination.TrackTopology.TrackBegin.BufferStop = new BufferStop { Id = source.BeginNode.Id, Code = source.BeginNode.Code, Name = source.BeginNode.Name, Description = source.BeginNode.Description };
            }
            else if (source.BeginType == TrackNodeType.OpenEnd)
            {
                destination.TrackTopology.TrackBegin.OpenEnd = new OpenEnd { Id = source.BeginNode.Id, Code = source.BeginNode.Code, Name = source.BeginNode.Name, Description = source.BeginNode.Description };
            }

            if (source.EndType == TrackNodeType.BufferStop)
            {
                destination.TrackTopology.TrackEnd.BufferStop = new BufferStop { Id = source.EndNode.Id, Code = source.EndNode.Code, Name = source.EndNode.Name, Description = source.EndNode.Description };
            }
            else if (source.EndType == TrackNodeType.OpenEnd)
            {
                destination.TrackTopology.TrackEnd.OpenEnd = new OpenEnd { Id = source.EndNode.Id, Code = source.EndNode.Code, Name = source.EndNode.Name, Description = source.EndNode.Description };
            }

            // Visualization
            var trackVis = new TrackVis { Ref = destination.Id };
            context.MainLineVis.TrackVisList.Add(trackVis);

            trackVis.TrackElementVisList.Add(new TrackElementVis
            {
                Ref = destination.TrackTopology.TrackBegin.Id,
                Position = new VisualizationPosition { X = source.X, Y = source.Y }
            });

            trackVis.TrackElementVisList.Add(new TrackElementVis
            {
                Ref = destination.TrackTopology.TrackEnd.Id,
                Position = new VisualizationPosition { X = source.X2, Y = source.Y2 }
            });

            if (isCurved && ctv != null)
            {
                trackVis.TrackElementVisList.Add(new TrackElementVis
                {
                    Ref = $"{destination.Id}_mid",
                    Position = new VisualizationPosition { X = ctv.MX, Y = ctv.MY }
                });
            }

            // Map OCS Elements (Signals and Borders)
            var boundSignals = context.Document.Elements.OfType<SignalViewModel>().Where(s => s.RelatedTrackId == source.Id).ToList();
            var boundBorders = context.Document.Elements.OfType<TrackCircuitBorderViewModel>().Where(b => b.RelatedTrackId == source.Id).ToList();

            if (boundSignals.Any() || boundBorders.Any())
            {
                destination.OcsElements = new OcsElements();

                if (boundSignals.Any())
                {
                    destination.OcsElements.Signals = new Signals();
                    foreach (var sigVm in boundSignals)
                    {
                        double dist = Math.Sqrt(Math.Pow(sigVm.X - source.X, 2) + Math.Pow(sigVm.Y - source.Y, 2));
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
                        destination.OcsElements.Signals.SignalList.Add(signal);

                        trackVis.TrackElementVisList.Add(new TrackElementVis
                        {
                            Ref = signal.Id,
                            Position = new VisualizationPosition { X = sigVm.X, Y = sigVm.Y }
                        });
                    }
                }

                if (boundBorders.Any())
                {
                    destination.OcsElements.TrainDetectionElements = new TrainDetectionElements();
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
                        destination.OcsElements.TrainDetectionElements.TrackCircuitBorderList.Add(border);

                        trackVis.TrackElementVisList.Add(new TrackElementVis
                        {
                            Ref = border.Id,
                            Position = new VisualizationPosition { X = borderVm.X, Y = borderVm.Y }
                        });
                    }
                }
            }
        }

        public void MapToViewModel(Track source, TrackViewModel destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Type = source.Type;
            destination.MainDir = source.MainDir;
            destination.Code = source.Code;

            // Handled externally during full document parse:
            // X, Y, X2, Y2 (From Visualizations Map)
            // BeginType, EndType, Connection logic
            // Switches
            // Signals
            // Borders
        }
    }
}
