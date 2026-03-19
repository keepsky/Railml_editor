using RailmlEditor.Models;
using RailmlEditor.ViewModels.Elements;
using System;
using System.Linq;

namespace RailmlEditor.Services.Mappers
{
    public class SwitchMapper : IRailmlElementMapper<SwitchViewModel, Switch>
    {
        public void MapToRailml(SwitchViewModel source, Switch destination, MappingContext context)
        {
            // Note: In the current architecture, Switches are deeply embedded in TrackTopology.Connections
            // The serialization to RailML XML logic for Switches is handled heavily by RailmlTopologyBuilder.
            // This mapper handles mapping the visualization portion of the switch.

            destination.Id = source.Id;
            destination.AdditionalName = new AdditionalName { Name = source.Name };
            destination.TrackContinueCourse = source.TrackContinueCourse;
            destination.NormalPosition = source.NormalPosition;
            
            // Switch Vis
            context.MainVis.ObjectVisList.Add(new ObjectVis
            {
                Ref = source.Id,
                Position = new VisualizationPosition { X = source.X, Y = source.Y }
            });
        }

        public void MapToViewModel(Switch source, SwitchViewModel destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.AdditionalName?.Name;
            destination.TrackContinueCourse = source.TrackContinueCourse ?? "straight";
            destination.NormalPosition = source.NormalPosition ?? "straight";

            // If we have a visualization map, use it. Try lookup in context.
            // Note: the coordinates and topological linking are primarily parsed during Track deserialization 
            // inside the Track connection loops in the document loader.
            
            // Topological reconstruction happens externally for now.
        }
    }
}
