using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using System.Collections.Generic;

namespace RailmlEditor.Services.Mappers
{
    public class MappingContext
    {
        public MainViewModel ViewModel { get; }
        public DocumentViewModel Document { get; }
        public Railml Railml { get; set; } = null!;
        public Infrastructure Infrastructure => Railml.Infrastructure;
        public LineVis MainLineVis { get; set; } = null!;
        public Visualization MainVis { get; set; } = null!;

        // Shared Dictionaries for Mapping
        public Dictionary<string, TrackViewModel> TrackLookup { get; } = new();
        public Dictionary<string, SwitchViewModel> SwitchLookup { get; } = new();
        public Dictionary<string, SignalViewModel> SignalLookup { get; } = new();

        public MappingContext(MainViewModel viewModel, DocumentViewModel document)
        {
            ViewModel = viewModel;
            Document = document;
        }

        public string GetRailmlId(string? prefix)
        {
            return ViewModel.GetNextId(prefix ?? "id");
        }
    }
}
