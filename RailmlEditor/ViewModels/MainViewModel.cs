using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RailmlEditor.Models;

namespace RailmlEditor.ViewModels
{
    public abstract class BaseElementViewModel : ObservableObject
    {
        private double _x;
        private double _y;
        private string _id;
        private bool _isSelected;

        public virtual double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public virtual double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public abstract string TypeName { get; }
    }

    public class TrackViewModel : BaseElementViewModel
    {
        public override string TypeName => "Track";
        
        // Collections for ComboBox
        public System.Collections.Generic.List<string> AvailableTypes { get; } = new System.Collections.Generic.List<string> 
        { 
            "mainTrack", "secondaryTrack", "connectingTrack", "sidingTrack", "stationTrack" 
        };

        public bool IsCurved => this is CurvedTrackViewModel;
        

        public System.Collections.Generic.List<string> AvailableMainDirs { get; } = new System.Collections.Generic.List<string> 
        { 
            "up", "down", "none" 
        };

        public ICommand FlipHorizontallyCommand { get; }
        public ICommand FlipVerticallyCommand { get; }


        public TrackViewModel()
        {
            FlipHorizontallyCommand = new RelayCommand(_ => FlipHorizontally());
            FlipVerticallyCommand = new RelayCommand(_ => FlipVertically());
        }

        protected virtual void FlipHorizontally()
        {
            double tempX = X;
            X = X2;
            X2 = tempX;
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(X2));
            OnPropertyChanged(nameof(Length));
        }

        protected virtual void FlipVertically()
        {
            // By default, do nothing or only for curved tracks if required.
            // But user said: "Flip Vertically only for code=corner"
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string? _type;
        public string? Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string? _mainDir;
        public string? MainDir
        {
            get => _mainDir;
            set => SetProperty(ref _mainDir, value);
        }

        private string? _code;
        public string? Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        
        public override double X
        {
            get => base.X;
            set
            {
                if (base.X != value)
                {
                    base.X = value;
                    OnPropertyChanged(nameof(X));
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        public override double Y
        {
            get => base.Y;
            set
            {
                if (base.Y != value)
                {
                    base.Y = value;
                    OnPropertyChanged(nameof(Y));
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        // End Point relative to X,Y? No, let's store absolute properties.
        private double _x2;
        public virtual double X2
        {
            get => _x2;
            set 
            {
                if (SetProperty(ref _x2, value))
                {
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        private double _y2;
        public virtual double Y2
        {
            get => _y2;
            set 
            {
                if (SetProperty(ref _y2, value))
                {
                    OnPropertyChanged(nameof(Length));
                }
            }
        }

        // Length is now derived or updates X2?
        // Let's make Length read-only derived, or if set, it updates X2 (assuming horizontal extension).
        public virtual double Length
        {
            get => System.Math.Sqrt(System.Math.Pow(X2 - X, 2) + System.Math.Pow(Y2 - Y, 2));
            set
            {
                // If setting length, assume extending horizontally from X
                X2 = X + value;
                Y2 = Y;
                OnPropertyChanged(nameof(Length));
            }
        }
    }

    public class SwitchViewModel : BaseElementViewModel
    {
        public override string TypeName => "Switch";
        
        [ReadOnly(true)]
        public override double X
        {
            get => base.X;
            set => base.X = value;
        }

        [ReadOnly(true)]
        public override double Y
        {
            get => base.Y;
            set => base.Y = value;
        }

        private double? _mx;
        public double? MX
        {
            get => _mx;
            set => SetProperty(ref _mx, value);
        }

        private double? _my;
        public double? MY
        {
            get => _my;
            set => SetProperty(ref _my, value);
        }

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
        public string TrackId { get; set; }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

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

        private void Track_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
        public SwitchViewModel Switch { get; set; }
        public List<TrackViewModel> Candidates { get; set; }
        public Action<TrackViewModel?> Callback { get; set; }
    }

    public class SignalViewModel : BaseElementViewModel
    {
        public override string TypeName => "Signal";
        
        // Collections for ComboBox
        public System.Collections.Generic.List<string> AvailableTypes { get; } = new System.Collections.Generic.List<string> 
        { 
            "main", "distant", "repeater", "shunting" 
        };

        public System.Collections.Generic.List<string> AvailableFunctions { get; } = new System.Collections.Generic.List<string> 
        { 
            "exit", "home", "blocking" 
        };

        private string? _type;
        public string? Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string? _function;
        public string? Function
        {
            get => _function;
            set => SetProperty(ref _function, value);
        }
        
        private string? _relatedTrackId;
        public string? RelatedTrackId
        {
            get => _relatedTrackId;
            set => SetProperty(ref _relatedTrackId, value);
        }

        public System.Collections.Generic.List<string> AvailableDirections { get; } = new System.Collections.Generic.List<string> { "up", "down" };
        
        private string _direction = "up";
        public string Direction
        {
            get => _direction;
            set 
            {
                if (SetProperty(ref _direction, value))
                {
                    OnPropertyChanged(nameof(IsFlipped));
                }
            }
        }

        public bool IsFlipped
        {
            get => _direction == "down";
            set => Direction = value ? "down" : "up";
        }
    }


    public class SwitchPositionViewModel : ObservableObject
    {
        private string _switchRef;
        public string SwitchRef { get => _switchRef; set => SetProperty(ref _switchRef, value); }

        private string _switchPosition = "normal";
        public string SwitchPosition { get => _switchPosition; set => SetProperty(ref _switchPosition, value); }

        public List<string> AvailablePositions { get; } = new() { "normal", "reverse" };

        public ICommand RemoveCommand { get; set; }
    }

    public class RouteViewModel : BaseElementViewModel
    {
        public override string TypeName => "Route";

        private string _code;
        public string Code { get => _code; set => SetProperty(ref _code, value); }

        private string _description;
        public string Description { get => _description; set => SetProperty(ref _description, value); }

        private string _approachPointRef;
        public string ApproachPointRef { get => _approachPointRef; set => SetProperty(ref _approachPointRef, value); }

        private string _entryRef;
        public string EntryRef { get => _entryRef; set => SetProperty(ref _entryRef, value); }

        private string _exitRef;
        public string ExitRef { get => _exitRef; set => SetProperty(ref _exitRef, value); }

        private string _overlapEndRef;
        public string OverlapEndRef { get => _overlapEndRef; set => SetProperty(ref _overlapEndRef, value); }

        private string _proceedSpeed = "R";
        public string ProceedSpeed { get => _proceedSpeed; set => SetProperty(ref _proceedSpeed, value); }

        private bool _releaseTriggerHead;
        public bool ReleaseTriggerHead { get => _releaseTriggerHead; set => SetProperty(ref _releaseTriggerHead, value); }

        private string _releaseTriggerRef;
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
            AddSwitchPositionCommand = new RelayCommand(_ => SwitchAndPositions.Add(new SwitchPositionViewModel { RemoveCommand = new RelayCommand(p => SwitchAndPositions.Remove(p as SwitchPositionViewModel)) }));
            AddOverlapSwitchPositionCommand = new RelayCommand(_ => OverlapSwitchAndPositions.Add(new SwitchPositionViewModel { RemoveCommand = new RelayCommand(p => OverlapSwitchAndPositions.Remove(p as SwitchPositionViewModel)) }));
            AddReleaseSectionCommand = new RelayCommand(_ => ReleaseSections.Add(new ReleaseSectionViewModel { RemoveCommand = new RelayCommand(p => ReleaseSections.Remove(p as ReleaseSectionViewModel)) }));
        }

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
    }

    public class ReleaseSectionViewModel : ObservableObject
    {
        private string _trackRef;
        public string TrackRef { get => _trackRef; set => SetProperty(ref _trackRef, value); }

        private bool _flankProtection;
        public bool FlankProtection { get => _flankProtection; set => SetProperty(ref _flankProtection, value); }

        public ICommand RemoveCommand { get; set; }

        public List<bool> AvailableProtections { get; } = new() { true, false };
    }

    public class CategoryViewModel : ObservableObject
    {
        public string Title { get; set; }
        public ObservableCollection<BaseElementViewModel> Items { get; } = new ObservableCollection<BaseElementViewModel>();
    }

    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<BaseElementViewModel> Elements { get; } = new ObservableCollection<BaseElementViewModel>();
        public ObservableCollection<BaseElementViewModel> SelectedElements { get; } = new ObservableCollection<BaseElementViewModel>();

        public ObservableCollection<CategoryViewModel> TreeCategories { get; } = new ObservableCollection<CategoryViewModel>();

        private BaseElementViewModel? _selectedElement;
        public BaseElementViewModel? SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        private BulkEditViewModel? _bulkEdit;
        public BulkEditViewModel? BulkEdit
        {
            get => _bulkEdit;
            set => SetProperty(ref _bulkEdit, value);
        }

        public event Action<SwitchBranchInfo>? PrincipleTrackSelectionRequested;

        public ICommand SelectCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }

        private List<BaseElementViewModel> _clipboard = new List<BaseElementViewModel>();

        public MainViewModel()
        {
            InitializeTreeCategories();
            Elements.CollectionChanged += Elements_CollectionChanged;

            SelectCommand = new RelayCommand(param => 
            {
                if (param is BaseElementViewModel vm)
                {
                    bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                    if (!isMulti)
                    {
                        foreach (var el in Elements) el.IsSelected = (el == vm);
                    }
                    else
                    {
                        vm.IsSelected = !vm.IsSelected;
                    }
                }
            });

            DeleteCommand = new RelayCommand(_ => DeleteSelected());
            CopyCommand = new RelayCommand(_ => CopySelected());
            PasteCommand = new RelayCommand(_ => PasteElements());

            // Test Data
            // Test Data Removed
        }


        private void DeleteSelected()
        {
            var toRemove = SelectedElements.ToList();
            foreach (var el in toRemove)
            {
                Elements.Remove(el);
            }
        }

        private void CopySelected()
        {
            _clipboard.Clear();
            foreach (var el in SelectedElements)
            {
                _clipboard.Add(CloneElement(el));
            }
        }

        private void PasteElements()
        {
            if (!_clipboard.Any()) return;

            // Clear current selection to select newly pasted items
            foreach (var el in Elements) el.IsSelected = false;

            foreach (var el in _clipboard)
            {
                var clone = CloneElement(el);
                
                // Offset position
                clone.X += 20;
                clone.Y += 20;
                
                if (clone is TrackViewModel track)
                {
                    track.X2 += 20;
                    track.Y2 += 20;
                    if (track is CurvedTrackViewModel curved)
                    {
                        curved.MX += 20;
                        curved.MY += 20;
                    }
                    clone.Id = GetNextId("T");
                }
                else if (clone is SignalViewModel signal)
                {
                    clone.Id = GetNextId("S");
                }
                else if (clone is SwitchViewModel sw)
                {
                    clone.Id = GetNextId("P");
                    if (sw.MX.HasValue) sw.MX += 20;
                    if (sw.MY.HasValue) sw.MY += 20;
                }

                clone.IsSelected = true;
                Elements.Add(clone);
            }

            // Update clipboard for next paste (cumulative offset)
            for (int i = 0; i < _clipboard.Count; i++)
            {
                _clipboard[i].X += 20;
                _clipboard[i].Y += 20;
                if (_clipboard[i] is TrackViewModel t)
                {
                    t.X2 += 20;
                    t.Y2 += 20;
                    if (t is CurvedTrackViewModel c)
                    {
                        c.MX += 20;
                        c.MY += 20;
                    }
                }
                else if (_clipboard[i] is SwitchViewModel sw)
                {
                    if (sw.MX.HasValue) sw.MX += 20;
                    if (sw.MY.HasValue) sw.MY += 20;
                }
            }
        }

        private BaseElementViewModel CloneElement(BaseElementViewModel el)
        {
            if (el is CurvedTrackViewModel curved)
            {
                return new CurvedTrackViewModel
                {
                    Id = curved.Id,
                    Name = curved.Name,
                    Description = curved.Description,
                    Type = curved.Type,
                    MainDir = curved.MainDir,
                    X = curved.X,
                    Y = curved.Y,
                    X2 = curved.X2,
                    Y2 = curved.Y2,
                    MX = curved.MX,
                    MY = curved.MY
                };
            }
            if (el is TrackViewModel track)
            {
                return new TrackViewModel
                {
                    Id = track.Id,
                    Name = track.Name,
                    Description = track.Description,
                    Type = track.Type,
                    MainDir = track.MainDir,
                    X = track.X,
                    Y = track.Y,
                    X2 = track.X2,
                    Y2 = track.Y2
                };
            }
            if (el is SignalViewModel signal)
            {
                return new SignalViewModel
                {
                    Id = signal.Id,
                    Name = signal.Name,
                    Type = signal.Type,
                    Function = signal.Function,
                    Direction = signal.Direction,
                    RelatedTrackId = signal.RelatedTrackId,
                    X = signal.X,
                    Y = signal.Y
                };
            }
            if (el is SwitchViewModel sw)
            {
                return new SwitchViewModel
                {
                    Id = sw.Id,
                    Name = sw.Name,
                    X = sw.X,
                    Y = sw.Y,
                    MX = sw.MX,
                    MY = sw.MY
                };
            }
            if (el is RouteViewModel r)
            {
                var nr = new RouteViewModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Code = r.Code,
                    Description = r.Description,
                    ApproachPointRef = r.ApproachPointRef,
                    EntryRef = r.EntryRef,
                    ExitRef = r.ExitRef,
                    OverlapEndRef = r.OverlapEndRef,
                    ProceedSpeed = r.ProceedSpeed,
                    ReleaseTriggerHead = r.ReleaseTriggerHead,
                    ReleaseTriggerRef = r.ReleaseTriggerRef
                };
                foreach (var s in r.SwitchAndPositions)
                    nr.SwitchAndPositions.Add(new SwitchPositionViewModel { SwitchRef = s.SwitchRef, SwitchPosition = s.SwitchPosition, RemoveCommand = new RelayCommand(p => nr.SwitchAndPositions.Remove(p as SwitchPositionViewModel)) });
                foreach (var s in r.OverlapSwitchAndPositions)
                    nr.OverlapSwitchAndPositions.Add(new SwitchPositionViewModel { SwitchRef = s.SwitchRef, SwitchPosition = s.SwitchPosition, RemoveCommand = new RelayCommand(p => nr.OverlapSwitchAndPositions.Remove(p as SwitchPositionViewModel)) });
                foreach (var s in r.ReleaseSections)
                    nr.ReleaseSections.Add(new ReleaseSectionViewModel { TrackRef = s.TrackRef, FlankProtection = s.FlankProtection, RemoveCommand = new RelayCommand(p => nr.ReleaseSections.Remove(p as ReleaseSectionViewModel)) });
                return nr;
            }
            return null;
        }

        private void InitializeTreeCategories()
        {
            TreeCategories.Add(new CategoryViewModel { Title = "Track" });
            TreeCategories.Add(new CategoryViewModel { Title = "Signal" });
            TreeCategories.Add(new CategoryViewModel { Title = "Point" });
            TreeCategories.Add(new CategoryViewModel { Title = "Route" });
        }

        private void Elements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
             if (e.Action == NotifyCollectionChangedAction.Reset)
             {
                 foreach(var cat in TreeCategories) cat.Items.Clear();
             }
             
             if (e.NewItems != null)
             {
                 foreach(BaseElementViewModel item in e.NewItems)
                 {
                     AddToCategory(item);
                 }
             }
             if (e.OldItems != null)
             {
                 foreach(BaseElementViewModel item in e.OldItems)
                 {
                     RemoveFromCategory(item);
                 }
             }
        }

        private void AddToCategory(BaseElementViewModel item)
        {
             string catTitle = "";
             if (item is TrackViewModel || item is CurvedTrackViewModel) catTitle = "Track";
             else if (item is SignalViewModel) catTitle = "Signal";
             else if (item is SwitchViewModel) catTitle = "Point";
             else if (item is RouteViewModel) catTitle = "Route";
             
             var cat = TreeCategories.FirstOrDefault(c => c.Title == catTitle);
             cat?.Items.Add(item);
             
             item.PropertyChanged += Element_PropertyChanged;
        }

        private void RemoveFromCategory(BaseElementViewModel item)
        {
             item.PropertyChanged -= Element_PropertyChanged;
             if (SelectedElements.Contains(item))
             {
                 SelectedElements.Remove(item);
                 UpdateSelectionState();
             }

             foreach(var cat in TreeCategories)
             {
                 if (cat.Items.Contains(item))
                 {
                     cat.Items.Remove(item);
                     break;
                 }
             }
        }

        private void Element_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseElementViewModel.IsSelected))
            {
                if (sender is BaseElementViewModel vm)
                {
                    if (vm.IsSelected)
                    {
                        if (!SelectedElements.Contains(vm)) SelectedElements.Add(vm);
                    }
                    else
                    {
                        SelectedElements.Remove(vm);
                    }
                    UpdateSelectionState();
                }
            }
            else if (e.PropertyName == nameof(SignalViewModel.Direction)) // Handle Direction Change
            {
                if (sender is SignalViewModel signal && !string.IsNullOrEmpty(signal.RelatedTrackId))
                {
                    var track = Elements.FirstOrDefault(el => el.Id == signal.RelatedTrackId) as TrackViewModel;
                    if (track != null)
                    {
                        if (signal.Direction == "down")
                        {
                            signal.X = track.X2;
                            signal.Y = track.Y2 + 15; // TrackEnd + Offset
                        }
                        else
                        {
                            signal.X = track.X;
                            signal.Y = track.Y - 15; // TrackBegin - Offset (Height 10 + 5 Gap)
                        }
                    }
                }
            }
        }

        private void UpdateSelectionState()
        {
            if (SelectedElements.Count == 1)
            {
                SelectedElement = SelectedElements[0];
                BulkEdit = null;
            }
            else if (SelectedElements.Count > 1)
            {
                SelectedElement = null;
                BulkEdit = new BulkEditViewModel(SelectedElements);
            }
            else
            {
                SelectedElement = null;
                BulkEdit = null;
            }
        }
        public void UpdateProximitySwitches()
        {
            var tracks = Elements.OfType<TrackViewModel>().ToList();
            var points = new List<(double X, double Y, string TrackId, bool isEnd)>();

            foreach (var t in tracks)
            {
                points.Add((t.X, t.Y, t.Id, false));
                points.Add((t.X2, t.Y2, t.Id, true));
            }

            var clusters = new List<(double X, double Y, List<(string TrackId, bool isEnd)> Members)>();
            var processedIndices = new HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (processedIndices.Contains(i)) continue;

                var members = new List<(string TrackId, bool isEnd)> { (points[i].TrackId, points[i].isEnd) };
                double sumX = points[i].X;
                double sumY = points[i].Y;
                processedIndices.Add(i);

                for (int j = i + 1; j < points.Count; j++)
                {
                    if (processedIndices.Contains(j)) continue;

                    double dist = Math.Sqrt(Math.Pow(points[i].X - points[j].X, 2) + Math.Pow(points[i].Y - points[j].Y, 2));
                    if (dist < 5.0)
                    {
                        sumX += points[j].X;
                        sumY += points[j].Y;
                        members.Add((points[j].TrackId, points[j].isEnd));
                        processedIndices.Add(j);
                    }
                }

                if (members.Count > 2)
                {
                    // Check if Scenario 1 or 2
                    // Scenario 1: One End, multiple Begins
                    // Scenario 2: One Begin, multiple Ends
                    int endCount = members.Count(m => m.isEnd);
                    int beginCount = members.Count(m => !m.isEnd);

                    if ((endCount == 1 && beginCount >= 2) || (beginCount == 1 && endCount >= 2))
                    {
                        clusters.Add((sumX / members.Count, sumY / members.Count, members));
                    }
                }
            }

            var existingSwitches = Elements.OfType<SwitchViewModel>().ToList();
            var switchesToRemove = existingSwitches.Where(sw => !clusters.Any(c => Math.Sqrt(Math.Pow(sw.X - c.X, 2) + Math.Pow(sw.Y - c.Y, 2)) < 10.0)).ToList();

            foreach (var sw in switchesToRemove) Elements.Remove(sw);

            int maxId = 0;
            foreach (var sw in Elements.OfType<SwitchViewModel>())
            {
                if (sw.Id.StartsWith("P") && int.TryParse(sw.Id.Substring(1), out int num))
                    if (num > maxId) maxId = num;
            }

            foreach (var c in clusters)
            {
                var sw = Elements.OfType<SwitchViewModel>().FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - c.X, 2) + Math.Pow(s.Y - c.Y, 2)) < 10.0);
                if (sw == null)
                {
                    maxId++;
                    sw = new SwitchViewModel
                    {
                        Id = $"P{maxId:D3}",
                        Name = $"P{maxId:D3}",
                        X = c.X,
                        Y = c.Y
                    };

                    int endCount = c.Members.Count(m => m.isEnd);
                    int beginCount = c.Members.Count(m => !m.isEnd);
                    sw.IsScenario1 = (endCount == 1);

                    var enteringMember = sw.IsScenario1 ? c.Members.First(m => m.isEnd) : c.Members.First(m => !m.isEnd);
                    var candidateMembers = sw.IsScenario1 ? c.Members.Where(m => !m.isEnd).ToList() : c.Members.Where(m => m.isEnd).ToList();

                    var candidates = candidateMembers.Select(m => Elements.OfType<TrackViewModel>().First(t => t.Id == m.TrackId)).ToList();

                    PrincipleTrackSelectionRequested?.Invoke(new SwitchBranchInfo
                    {
                        Switch = sw,
                        Candidates = candidates,
                        Callback = (principle) =>
                        {
                            if (principle != null)
                            {
                                sw.EnteringTrackId = enteringMember.TrackId;
                                sw.PrincipleTrackId = principle.Id;
                                sw.DivergingTrackIds.Clear();
                                sw.DivergingConnections.Clear();
                                foreach (var cand in candidates)
                                {
                                    if (cand.Id != principle.Id)
                                    {
                                        sw.DivergingTrackIds.Add(cand.Id);
                                        sw.DivergingConnections.Add(new DivergingConnectionViewModel
                                        {
                                            TrackId = cand.Id,
                                            TargetTrack = cand
                                        });
                                    }
                                }
                                Elements.Add(sw);
                            }
                            else
                            {
                                // Notify UI to rollback move (handled in MainWindow.xaml.cs)
                            }
                        }
                    });
                }
                else
                {
                    // Existing switch, update its position just in case
                    sw.X = c.X;
                    sw.Y = c.Y;
                }
            }
        }
        public string GetNextId(string prefix)
        {
            int max = 0;
            foreach (var el in Elements)
            {
                if (el.Id != null && el.Id.StartsWith(prefix) && int.TryParse(el.Id.Substring(prefix.Length), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return $"{prefix}{max + 1:D3}";
        }
    }
}
