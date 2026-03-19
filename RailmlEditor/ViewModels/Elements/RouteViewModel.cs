using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RailmlEditor.Utils;


namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 분기기가 어떤 방향(직진/곡선)으로 맞춰져 있는지를 나타내는 보조 클래스입니다.
    /// Route(경로)를 설정할 때, "이 경로를 지나가려면 1번 스위치는 직진이어야 하고 2번 스위치는 역방향이어야 해"라는 조건을 담기 위해 쓰입니다.
    /// </summary>
    public class SwitchPositionViewModel : ObservableObject
    {
        private string? _switchRef;
        public string? SwitchRef { get => _switchRef; set => SetProperty(ref _switchRef, value); }

        private string _switchPosition = "normal";
        public string SwitchPosition { get => _switchPosition; set => SetProperty(ref _switchPosition, value); }

        public List<string> AvailablePositions { get; } = new() { "normal", "reverse" };

        public ICommand? RemoveCommand { get; set; }
    }

    /// <summary>
    /// 기차가 A 지점에서 B 지점까지 이동하기 위해 지나가는 '경로(Route)'를 정의하는 클래스입니다.
    /// (시작 신호기부터 끝 신호기까지, 도중에 지나가는 분기기의 방향 설정 등을 포함합니다.)
    /// </summary>
    public class RouteViewModel : BaseElementViewModel
    {
        public override string TypeName => "Route";

        private string _code = string.Empty;
        public string Code { get => _code; set => SetProperty(ref _code, value); }

        private string _description = string.Empty;
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string _approachPointRef = string.Empty;
        /// <summary>경로에 접근하는 지점 (주로 신호기 ID)</summary>
        public string ApproachPointRef { get => _approachPointRef; set => SetProperty(ref _approachPointRef, value); }
        
        private string _entryRef = string.Empty;
        /// <summary>경로가 본격적으로 시작되는 지점 (주로 출발 신호기 ID)</summary>
        public string EntryRef { get => _entryRef; set => SetProperty(ref _entryRef, value); }

        private string _exitRef = string.Empty;
        /// <summary>경로가 끝나는 지점 (주로 다음 도착 신호기 ID)</summary>
        public string ExitRef { get => _exitRef; set => SetProperty(ref _exitRef, value); }

        private string _overlapEndRef = string.Empty;
        /// <summary>기차가 제동을 걸지 못하고 미끄러졌을 때를 대비한 여유 안전 구간(오버랩)의 끝 지점</summary>
        public string OverlapEndRef { get => _overlapEndRef; set => SetProperty(ref _overlapEndRef, value); }

        private string _proceedSpeed = "R";
        public string ProceedSpeed { get => _proceedSpeed; set => SetProperty(ref _proceedSpeed, value); }

        private bool _releaseTriggerHead;
        public bool ReleaseTriggerHead { get => _releaseTriggerHead; set => SetProperty(ref _releaseTriggerHead, value); }

        private string _releaseTriggerRef = string.Empty;
        /// <summary>어느 지점을 통과하면 이 경로에 걸린 잠금을 해제할지 결정하는 신호기/구간 ID입니다.</summary>
        public string ReleaseTriggerRef { get => _releaseTriggerRef; set => SetProperty(ref _releaseTriggerRef, value); }

        /// <summary>이 경로를 통과하기 위해 필수적으로 지정된 방향으로 맞춰져야 하는 분기기(스위치)들 목록입니다.</summary>
        public ObservableCollection<SwitchPositionViewModel> SwitchAndPositions { get; } = new();
        
        /// <summary>여유 구간(Overlap) 안에 포함되어 있어 보호가 필요한 분기기들 목록입니다.</summary>
        public ObservableCollection<SwitchPositionViewModel> OverlapSwitchAndPositions { get; } = new();
        
        /// <summary>기차가 지나가고 나면 순차적으로 잠금을 풀어줄 수 있는 구간(Track Circuit)들 목록입니다.</summary>
        public ObservableCollection<ReleaseSectionViewModel> ReleaseSections { get; } = new();

        public ICommand AddSwitchPositionCommand { get; }
        public ICommand AddOverlapSwitchPositionCommand { get; }
        public ICommand AddReleaseSectionCommand { get; }

        public List<string> AvailableProceedSpeeds { get; } = new() { "R", "YY", "Y", "YG", "G" };

        public RouteViewModel()
        {
            AddSwitchPositionCommand = new RelayCommand(_ => SwitchAndPositions.Add(new SwitchPositionViewModel { RemoveCommand = new RelayCommand(p => { if (p is SwitchPositionViewModel vm) SwitchAndPositions.Remove(vm); }) }));
            AddOverlapSwitchPositionCommand = new RelayCommand(_ => OverlapSwitchAndPositions.Add(new SwitchPositionViewModel { RemoveCommand = new RelayCommand(p => { if (p is SwitchPositionViewModel vm) OverlapSwitchAndPositions.Remove(vm); }) }));
            AddReleaseSectionCommand = new RelayCommand(_ => ReleaseSections.Add(new ReleaseSectionViewModel { RemoveCommand = new RelayCommand(p => { if (p is ReleaseSectionViewModel vm) ReleaseSections.Remove(vm); }) }));
        }
    }

    public class ReleaseSectionViewModel : ObservableObject
    {
        private string? _trackRef;
        public string? TrackRef { get => _trackRef; set => SetProperty(ref _trackRef, value); }

        private bool _flankProtection;
        public bool FlankProtection { get => _flankProtection; set => SetProperty(ref _flankProtection, value); }

        public List<bool> AvailableFlankProtections { get; } = new() { true, false };
        
        public ICommand? RemoveCommand { get; set; }
    }
}


