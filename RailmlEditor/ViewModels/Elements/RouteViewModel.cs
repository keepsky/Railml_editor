using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using RailmlEditor.Utils;


namespace RailmlEditor.ViewModels.Elements
{
    public class SwitchPositionViewModel : ObservableObject
    {
        private string? _switchRef;
        public string? SwitchRef { get => _switchRef; set => SetProperty(ref _switchRef, value); }

        private string _switchPosition = "normal";
        public string SwitchPosition { get => _switchPosition; set => SetProperty(ref _switchPosition, value); }

        public List<string> AvailablePositions { get; } = new() { "normal", "reverse" };

        public ICommand? RemoveCommand { get; set; }
    }

    public class RouteViewModel : BaseElementViewModel
    {
        public override string TypeName => "Route";

        private string _code = string.Empty;
        public string Code { get => _code; set => SetProperty(ref _code, value); }

        private string _description = string.Empty;
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string _approachPointRef = string.Empty;
        public string ApproachPointRef { get => _approachPointRef; set => SetProperty(ref _approachPointRef, value); }

        private string _entryRef = string.Empty;
        public string EntryRef { get => _entryRef; set => SetProperty(ref _entryRef, value); }

        private string _exitRef = string.Empty;
        public string ExitRef { get => _exitRef; set => SetProperty(ref _exitRef, value); }

        private string _overlapEndRef = string.Empty;
        public string OverlapEndRef { get => _overlapEndRef; set => SetProperty(ref _overlapEndRef, value); }

        private string _proceedSpeed = "R";
        public string ProceedSpeed { get => _proceedSpeed; set => SetProperty(ref _proceedSpeed, value); }

        private bool _releaseTriggerHead;
        public bool ReleaseTriggerHead { get => _releaseTriggerHead; set => SetProperty(ref _releaseTriggerHead, value); }

        private string _releaseTriggerRef = string.Empty;
        public string ReleaseTriggerRef { get => _releaseTriggerRef; set => SetProperty(ref _releaseTriggerRef, value); }

        public ObservableCollection<SwitchPositionViewModel> SwitchAndPositions { get; } = new();
        public ObservableCollection<SwitchPositionViewModel> OverlapSwitchAndPositions { get; } = new();
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


