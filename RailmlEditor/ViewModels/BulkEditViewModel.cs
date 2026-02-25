using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.ViewModels
{
    /// <summary>
    /// 여러 개의 선로나 신호기를 마우스로 드래그해서 한꺼번에 선택했을 때 표시되는 속성창(Property Window) 전용 뷰모델입니다.
    /// 선택된 요소들이 가진 공통된 값을 보여주거나, 이 창에서 값을 바꾸면 선택된 모든 요소들에게 일괄적으로 값을 적용(Bulk Edit)해주는 역할을 합니다.
    /// </summary>
    public class BulkEditViewModel : ObservableObject
    {
        private readonly List<BaseElementViewModel> _elements;

        public BulkEditViewModel(IEnumerable<BaseElementViewModel> elements)
        {
            _elements = elements.ToList();
        }

        public string DisplayName => $"{_elements.Count} items selected";

        private string? GetCommonValue(Func<BaseElementViewModel, string?> getter)
        {
            if (!_elements.Any()) return null;
            var first = getter(_elements.First());
            return _elements.All(e => getter(e) == first) ? first : null;
        }

        private void SetCommonValue(Action<BaseElementViewModel, string?> setter, string? value)
        {
            foreach (var e in _elements)
            {
                setter(e, value);
            }
        }

        public string? Name
        {
            get => GetCommonValue(e => e.Name);
            set { SetCommonValue((e, v) => e.Name = v, value); OnPropertyChanged(); }
        }

        // Common for Tracks
        public string? Description
        {
            get => GetCommonValue(e => (e as TrackViewModel)?.Description);
            set { SetCommonValue((e, v) => { if (e is TrackViewModel t) t.Description = v; }, value); OnPropertyChanged(); }
        }

        public string? Type
        {
            get => GetCommonValue(e => (e as TrackViewModel)?.Type);
            set { SetCommonValue((e, v) => { if (e is TrackViewModel t) t.Type = v; }, value); OnPropertyChanged(); }
        }

        public string? MainDir
        {
            get => GetCommonValue(e => (e as TrackViewModel)?.MainDir);
            set { SetCommonValue((e, v) => { if (e is TrackViewModel t) t.MainDir = v; }, value); OnPropertyChanged(); }
        }

        // Common for Signals
        public string? SignalType
        {
            get => GetCommonValue(e => (e as SignalViewModel)?.Type);
            set { SetCommonValue((e, v) => { if (e is SignalViewModel s) s.Type = v; }, value); OnPropertyChanged(); }
        }

        public string? SignalFunction
        {
            get => GetCommonValue(e => (e as SignalViewModel)?.Function);
            set { SetCommonValue((e, v) => { if (e is SignalViewModel s) s.Function = v; }, value); OnPropertyChanged(); }
        }

        public string? Direction
        {
            get => GetCommonValue(e => (e as SignalViewModel)?.Direction);
            set { SetCommonValue((e, v) => { if (e is SignalViewModel s) s.Direction = v ?? string.Empty; }, value); OnPropertyChanged(); }
        }

        // Helper for showing/hiding sections in Property Panel
        public bool HasTracks => _elements.Any(e => e is TrackViewModel);
        public bool HasSignals => _elements.Any(e => e is SignalViewModel);

        // Sources for ComboBoxes (taken from first available instance)
        public List<string>? AvailableTypes => (_elements.FirstOrDefault(e => e is TrackViewModel) as TrackViewModel)?.AvailableTypes;
        public List<string>? AvailableMainDirs => (_elements.FirstOrDefault(e => e is TrackViewModel) as TrackViewModel)?.AvailableMainDirs;
        public List<string>? SignalTypes => (_elements.FirstOrDefault(e => e is SignalViewModel) as SignalViewModel)?.AvailableTypes;
        public List<string>? SignalFunctions => (_elements.FirstOrDefault(e => e is SignalViewModel) as SignalViewModel)?.AvailableFunctions;
        public List<string>? Directions => (_elements.FirstOrDefault(e => e is SignalViewModel) as SignalViewModel)?.AvailableDirections;
    }
}

