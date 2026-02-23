using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels.Elements
{
    public class SwitchViewModel : BaseElementViewModel
    {
        public override string TypeName => "Switch";
        public double Pos { get; set; }

        private string _trackContinueCourse = "straight";
        public string TrackContinueCourse
        {
            get => _trackContinueCourse;
            set => SetProperty(ref _trackContinueCourse, value);
        }

        private string _normalPosition = "straight";
        public string NormalPosition
        {
            get => _normalPosition;
            set => SetProperty(ref _normalPosition, value);
        }

        public ObservableCollection<DivergingConnectionViewModel> DivergingConnections { get; } = new();

        public List<string> AvailableCourses { get; } = new() { "straight", "left", "right", "other" };

        // Logic data
        public string? PrincipleTrackId { get; set; }
        public string? EnteringTrackId { get; set; }
        public List<string> DivergingTrackIds { get; } = new();
        public bool IsScenario1 { get; set; } // true: End -> multiple Begins, false: Begin -> multiple Ends
    }

    public class DivergingConnectionViewModel : ObservableObject
    {
        public string? TrackId { get; set; }

        private string? _displayName;
        public string? DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        private string? _id;
        public string? Id { get => _id; set => SetProperty(ref _id, value); }

        private string? _ref;
        public string? Ref { get => _ref; set => SetProperty(ref _ref, value); }

        private string _orientation = "outgoing";
        public string Orientation { get => _orientation; set => SetProperty(ref _orientation, value); }

        private TrackViewModel? _targetTrack;
        public TrackViewModel? TargetTrack
        {
            get => _targetTrack;
            set
            {
                if (_targetTrack != null) _targetTrack.PropertyChanged -= Track_PropertyChanged;
                _targetTrack = value;
                if (_targetTrack != null)
                {
                    _targetTrack.PropertyChanged += Track_PropertyChanged;
                    UpdateDisplayName();
                }
            }
        }

        private void Track_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseElementViewModel.Name) || e.PropertyName == nameof(BaseElementViewModel.Id))
            {
                UpdateDisplayName();
            }
        }

        private void UpdateDisplayName()
        {
            if (TargetTrack != null)
            {
                DisplayName = $"{TargetTrack.Id}({TargetTrack.Name ?? "unnamed"})";
            }
        }

        private string _course = "straight";
        public string Course
        {
            get => _course;
            set => SetProperty(ref _course, value);
        }

        public List<string> AvailableCourses { get; } = new() { "straight", "left", "right" };
    }

    public class SwitchBranchInfo
    {
        public SwitchViewModel? Switch { get; set; }
        public List<TrackViewModel>? Candidates { get; set; }
        public Action<TrackViewModel?>? Callback { get; set; }
    }
}


