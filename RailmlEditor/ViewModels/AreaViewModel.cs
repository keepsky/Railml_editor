using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace RailmlEditor.ViewModels
{
    public class AreaViewModel : BaseElementViewModel
    {
        public override string TypeName => "Area";

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _type = "trackSection";
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public List<string> AvailableTypes { get; } = new() { "trackSection", "project" };

        public ObservableCollection<TrackCircuitBorderViewModel> Borders { get; } = new();

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
    }
}
