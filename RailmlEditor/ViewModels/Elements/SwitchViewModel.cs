using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 기차의 방향을 바꿔주는 분기기(스위치, 선로전환기)를 나타내는 뷰모델입니다.
    /// 어떤 선로(EnteringTrack)에서 들어와서 어떤 선로들(DivergingTracks)로 나갈 수 있는지를 관리합니다.
    /// </summary>
    public class SwitchViewModel : BaseElementViewModel
    {
        public override string TypeName => "Switch";
        public double Pos { get; set; }

        /// <summary>주 연결 선로(Straight/Left/Right 등)의 진행 방향입니다.</summary>
        private string _trackContinueCourse = "straight";
        public string TrackContinueCourse
        {
            get => _trackContinueCourse;
            set => SetProperty(ref _trackContinueCourse, value);
        }

        /// <summary>평상시 고정되어 있는 기본 방향(Normal Position)입니다.</summary>
        private string _normalPosition = "straight";
        public string NormalPosition
        {
            get => _normalPosition;
            set => SetProperty(ref _normalPosition, value);
        }

        public ObservableCollection<DivergingConnectionViewModel> DivergingConnections { get; } = new();

        public List<string> AvailableCourses { get; } = new() { "straight", "left", "right", "other" };

        // Logic data
        /// <summary>메인 줄기 선로의 ID</summary>
        public string? PrincipleTrackId { get; set; }
        /// <summary>분기기로 진입하는 선로의 ID</summary>
        public string? EnteringTrackId { get; set; }
        /// <summary>분기기에서 갈라져 나가는 여러 선로들의 ID 목록</summary>
        public List<string> DivergingTrackIds { get; } = new();
        
        /// <summary>
        /// true: 끝(End) 지점에서 여러 개의 시작(Begin) 지점으로 갈라짐
        /// false: 시작(Begin) 지점에서 여러 개의 끝(End) 지점으로 갈라짐
        /// </summary>
        public bool IsScenario1 { get; set; }
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


