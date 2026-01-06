using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RailmlEditor.Models;
using RailmlEditor.Utils;
using System.IO;
using System.Text.Json;

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

        private bool _showCoordinates;
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set => SetProperty(ref _showCoordinates, value);
        }

        public abstract string TypeName { get; }
    }

    public enum TrackNodeType
    {
        None,
        BufferStop,
        OpenEnd,
        Connection
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
            // This is TrackViewModel... oops. I need to edit MainViewModel!
            // Wait, I am editing MainViewModel.cs, but scrolling shows TrackViewModel constructor.
            // I need to find MainViewModel constructor.
            // Initialize Nodes
            BeginNode.Role = "Begin";
            BeginNode.Pos = 0;
            EndNode.Role = "End";
            EndNode.Pos = Length;
            Children.Add(BeginNode);
            Children.Add(EndNode);

            BeginNode.PropertyChanged += OnNodePropertyChanged;
            EndNode.PropertyChanged += OnNodePropertyChanged;
            this.PropertyChanged += OnTrackPropertyChanged;
        }

        private void OnTrackPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Id))
            {
                if (!string.IsNullOrEmpty(Id))
                {
                    BeginNode.Id = $"{Id}_begin";
                    EndNode.Id = $"{Id}_end";
                }
            }
            // Trigger connection check on geometry change
            if (e.PropertyName == nameof(X) || e.PropertyName == nameof(Y) || 
                e.PropertyName == nameof(X2) || e.PropertyName == nameof(Y2) ||
                e.PropertyName == nameof(CurvedTrackViewModel.MX) || e.PropertyName == nameof(CurvedTrackViewModel.MY))
            {
                // We need access to the MainViewModel instance or Elements collection to check others.
                // TrackViewModel doesn't know about MainViewModel.
                // We must handle this in MainViewModel.Elements_CollectionChanged -> Item.PropertyChanged (already there!)
            }
            if (e.PropertyName == nameof(Length))
            {
                EndNode.Pos = Length;
            }
        }

        private void OnNodePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrackNodeViewModel.NodeType))
            {
                if (sender == BeginNode && BeginType != BeginNode.NodeType)
                {
                    BeginType = BeginNode.NodeType;
                }
                else if (sender == EndNode && EndType != EndNode.NodeType)
                {
                    EndType = EndNode.NodeType;
                }
                // ID Generation logic removed as per request to revert to T_begin/end format
            }
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

        private string? _mainDir = "up";
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
                    double delta = value - base.X;
                    base.X = value;
                    OnPropertyChanged(nameof(X));
                    OnPropertyChanged(nameof(Length));

                    // Move Child Signals and Borders
                    if (Children != null)
                    {
                        foreach (var child in Children)
                        {
                            if (child is SignalViewModel signal)
                            {
                                signal.X += delta;
                            }
                            else if (child is TrackCircuitBorderViewModel border)
                            {
                                border.X += delta;
                            }
                        }
                        UpdateBorderAngles();
                    }
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
                    double delta = value - base.Y;
                    base.Y = value;
                    OnPropertyChanged(nameof(Y));
                    OnPropertyChanged(nameof(Length));

                    // Move Child Signals and Borders
                    if (Children != null)
                    {
                        foreach (var child in Children)
                        {
                            if (child is SignalViewModel signal)
                            {
                                signal.Y += delta;
                            }
                            else if (child is TrackCircuitBorderViewModel border)
                            {
                                border.Y += delta;
                            }
                        }
                        UpdateBorderAngles();
                    }
                }
            }
        }

        private double _x2;
        public virtual double X2
        {
            get => _x2;
            set 
            {
                if (SetProperty(ref _x2, value))
                {
                    OnPropertyChanged(nameof(Length));
                    UpdateBorderAngles();
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
                    UpdateBorderAngles();
                }
            }
        }

        private void UpdateBorderAngles()
        {
            if (Children == null) return;
            double angle = Math.Atan2(Y2 - Y, X2 - X) * 180 / Math.PI;
            foreach (var border in Children.OfType<TrackCircuitBorderViewModel>())
            {
                border.Angle = angle;
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

        public ObservableCollection<BaseElementViewModel> Children { get; } = new();

        public TrackNodeViewModel BeginNode { get; } = new TrackNodeViewModel();
        public TrackNodeViewModel EndNode { get; } = new TrackNodeViewModel();

        // Begin/End Configuration
        private TrackNodeType _beginType;
        public TrackNodeType BeginType
        {
            get => _beginType;
            set
            {
                if (SetProperty(ref _beginType, value))
                {
                    OnBeginTypeChanged();
                }
            }
        }

        private TrackNodeType _endType;
        public TrackNodeType EndType
        {
            get => _endType;
            set
            {
                if (SetProperty(ref _endType, value))
                {
                    OnEndTypeChanged();
                }
            }
        }

        // Connection State (to disable fields)
        private bool _hasBeginConnection;
        public bool HasBeginConnection
        {
            get => _hasBeginConnection;
            set
            {
                if (SetProperty(ref _hasBeginConnection, value))
                {
                    OnPropertyChanged(nameof(IsBeginEnabled));
                }
            }
        }

        private bool _hasEndConnection;
        public bool HasEndConnection
        {
            get => _hasEndConnection;
            set
            {
                if (SetProperty(ref _hasEndConnection, value))
                {
                    OnPropertyChanged(nameof(IsEndEnabled));
                }
            }
        }

        public bool IsBeginEnabled => !HasBeginConnection;
        public bool IsEndEnabled => !HasEndConnection;

        // Auto-ID Logic & Children Management
        private void OnBeginTypeChanged()
        {
            BeginNode.NodeType = BeginType;
            BeginNode.Role = "Begin";
            
            if (!Children.Contains(BeginNode)) Children.Insert(0, BeginNode); // Insert at top
        }

        private void OnEndTypeChanged()
        {
            EndNode.NodeType = EndType;
            EndNode.Role = "End";

            if (!Children.Contains(EndNode)) Children.Add(EndNode); // Add at bottom (or after Begin)
        }

        private string GenerateId(string prefix)
        {
            // Extract number from Track ID
            var match = System.Text.RegularExpressions.Regex.Match(Id ?? "", @"\d+$");
            string number = match.Success ? match.Value : "1";
            return $"{prefix}{number}";
        }

        public IEnumerable<TrackNodeType> AvailableTrackNodeTypes => System.Enum.GetValues(typeof(TrackNodeType)).Cast<TrackNodeType>();
    }

    public class SwitchViewModel : BaseElementViewModel
    {
        public override string TypeName => "Switch";

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

        private double _pos;
        public double Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
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

    public class InfrastructureViewModel : BaseElementViewModel
    {
        public override string TypeName => "Infrastructure";
        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
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

        public ObservableCollection<InfrastructureViewModel> TreeRoots { get; } = new ObservableCollection<InfrastructureViewModel>();
        
        private InfrastructureViewModel _activeInfrastructure;
        public InfrastructureViewModel ActiveInfrastructure
        {
            get => _activeInfrastructure;
            set => SetProperty(ref _activeInfrastructure, value);
        }

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
        public ICommand OpenTemplatesCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        private List<BaseElementViewModel> _clipboard = new List<BaseElementViewModel>();

        public UndoRedoManager History { get; } = new();

        public MainViewModel()
        {
            InitializeInfrastructure();
            Elements.CollectionChanged += Elements_CollectionChanged;
            History.StateChanged += (s, e) => { 
                (UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            UndoCommand = new RelayCommand(_ => History.Undo(), _ => History.CanUndo);
            RedoCommand = new RelayCommand(_ => History.Redo(), _ => History.CanRedo);

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

            OpenTemplatesCommand = new RelayCommand(_ => 
            {
                var win = new TemplatesWindow(this);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            // Test Data
            // Test Data Removed

            LoadCustomTemplates();
        }


        private void DeleteSelected()
        {
            var oldState = TakeSnapshot();
            var toRemove = SelectedElements.ToList();
            if (!toRemove.Any()) return;

            foreach (var el in toRemove)
            {
                Elements.Remove(el);
            }
            AddHistory(oldState);
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
            var oldState = TakeSnapshot();

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
                    clone.Id = GetNextId("tr");
                }
                else if (clone is SignalViewModel signal)
                {
                    clone.Id = GetNextId("sig");
                }
                else if (clone is SwitchViewModel sw)
                {
                    clone.Id = GetNextId("sw");
                }

                clone.IsSelected = true;
                Elements.Add(clone);
            }
            AddHistory(oldState);

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
                    // Sw coordinates already handled by base property increment in PasteElements if needed
                }
            }
        }

        // Custom Templates Storage
        private readonly string _templatesPath = "templates.json";
        public System.Collections.Generic.Dictionary<string, string> CustomTemplates { get; } = new System.Collections.Generic.Dictionary<string, string>();

        public void SaveCustomTemplates()
        {
            try
            {
                var json = JsonSerializer.Serialize(CustomTemplates, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_templatesPath, json);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving templates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadCustomTemplates()
        {
            try
            {
                if (File.Exists(_templatesPath))
                {
                    var json = File.ReadAllText(_templatesPath);
                    var loaded = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(json);
                    if (loaded != null)
                    {
                        CustomTemplates.Clear();
                        foreach (var kvp in loaded)
                        {
                            CustomTemplates[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading templates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddDoubleTrack(string filePath, Point? targetPos = null)
        {
            var oldState = TakeSnapshot();
            string finalPath = filePath;
            
            // Check if there is a custom template for this type
            // Extract key from filePath (e.g. "single.railml" -> "single")
            string templateKey = System.IO.Path.GetFileNameWithoutExtension(filePath).ToLower();
            
            var service = new RailmlEditor.Services.RailmlService();
            System.Collections.Generic.List<BaseElementViewModel> snippet = new System.Collections.Generic.List<BaseElementViewModel>();

            if (CustomTemplates.ContainsKey(templateKey) && !string.IsNullOrWhiteSpace(CustomTemplates[templateKey]))
            {
                // Use custom template
                snippet = service.LoadSnippetFromXml(CustomTemplates[templateKey], this);
            }
            else
            {
                // Fallback to file load
                if (!System.IO.File.Exists(finalPath))
                {
                    // Better path resolution: search up to project root
                    string currentDir = System.AppDomain.CurrentDomain.BaseDirectory;
                    string? candidate = null;
                    
                    // Also search relative to CWD
                    string workingDir = System.IO.Directory.GetCurrentDirectory();
                    
                    var searchDirs = new[] { workingDir, currentDir };
                    foreach (var baseDir in searchDirs)
                    {
                        string probeRoot = baseDir;
                        for (int i = 0; i < 5; i++)
                        {
                            string probe = System.IO.Path.Combine(probeRoot, filePath);
                            if (System.IO.File.Exists(probe))
                            {
                                candidate = probe;
                                break;
                            }
                            probeRoot = System.IO.Path.GetDirectoryName(probeRoot);
                            if (string.IsNullOrEmpty(probeRoot)) break;
                        }
                        if (candidate != null) break;
                    }
                    
                    if (candidate != null) finalPath = candidate;
                }
                snippet = service.LoadSnippet(finalPath, this);
            }

            if (snippet.Count > 0)
            {
                // Deselect current
                foreach (var el in Elements) el.IsSelected = false;

                if (targetPos.HasValue)
                {
                    // Calculate bounding box of snippet to center it at targetPos
                    double minX = double.MaxValue, minY = double.MaxValue;
                    double maxX = double.MinValue, maxY = double.MinValue;
                    
                    bool hasGeometry = false;
                    foreach (var el in snippet)
                    {
                        if (el.X != 0 || el.Y != 0) // Basic check for geometry
                        {
                            minX = Math.Min(minX, el.X);
                            minY = Math.Min(minY, el.Y);
                            maxX = Math.Max(maxX, el.X);
                            maxY = Math.Max(maxY, el.Y);
                            
                            if (el is TrackViewModel t)
                            {
                                minX = Math.Min(minX, t.X2);
                                minY = Math.Min(minY, t.Y2);
                                maxX = Math.Max(maxX, t.X2);
                                maxY = Math.Max(maxY, t.Y2);
                                
                                if (t is CurvedTrackViewModel c)
                                {
                                    minX = Math.Min(minX, c.MX);
                                    minY = Math.Min(minY, c.MY);
                                    maxX = Math.Max(maxX, c.MX);
                                    maxY = Math.Max(maxY, c.MY);
                                }
                            }
                            hasGeometry = true;
                        }
                    }
                    
                    if (hasGeometry)
                    {
                        double centerX = (minX + maxX) / 2.0;
                        double centerY = (minY + maxY) / 2.0;
                        
                        double dx = Math.Round((targetPos.Value.X - centerX) / 10.0) * 10.0;
                        double dy = Math.Round((targetPos.Value.Y - centerY) / 10.0) * 10.0;
                        
                        foreach (var el in snippet)
                        {
                            el.X += dx;
                            el.Y += dy;
                            if (el is TrackViewModel t)
                            {
                                t.X2 += dx;
                                t.Y2 += dy;
                                if (t is CurvedTrackViewModel c)
                                {
                                    c.MX += dx;
                                    c.MY += dy;
                                }
                            }
                            else if (el is SwitchViewModel sw)
                            {
                                // Sw coordinates already handled by base property increment above
                            }
                        }
                    }
                }

                foreach (var el in snippet)
                {
                    el.IsSelected = true;
                    Elements.Add(el);
                }
                UpdateSelectionState();
                AddHistory(oldState);
            }
        }

        private BaseElementViewModel CloneElement(BaseElementViewModel el)
        {
            if (el is CurvedTrackViewModel curved)
            {
                var newCurved = new CurvedTrackViewModel
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
                    MY = curved.MY,
                    ShowCoordinates = curved.ShowCoordinates,
                    BeginType = curved.BeginType,
                    EndType = curved.EndType
                };
                newCurved.BeginNode.Code = curved.BeginNode.Code;
                newCurved.BeginNode.Name = curved.BeginNode.Name;
                newCurved.BeginNode.Description = curved.BeginNode.Description;
                newCurved.EndNode.Code = curved.EndNode.Code;
                newCurved.EndNode.Name = curved.EndNode.Name;
                newCurved.EndNode.Description = curved.EndNode.Description;
                return newCurved;
            }
            if (el is TrackViewModel track)
            {
                var newTrack = new TrackViewModel
                {
                    Id = track.Id,
                    Name = track.Name,
                    Description = track.Description,
                    Type = track.Type,
                    MainDir = track.MainDir,
                    X = track.X,
                    Y = track.Y,
                    X2 = track.X2,
                    Y2 = track.Y2,
                    ShowCoordinates = track.ShowCoordinates,
                    BeginType = track.BeginType,
                    EndType = track.EndType
                };
                newTrack.BeginNode.Code = track.BeginNode.Code;
                newTrack.BeginNode.Name = track.BeginNode.Name;
                newTrack.BeginNode.Description = track.BeginNode.Description;
                newTrack.EndNode.Code = track.EndNode.Code;
                newTrack.EndNode.Name = track.EndNode.Name;
                newTrack.EndNode.Description = track.EndNode.Description;
                return newTrack;
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
                    Y = signal.Y,
                    ShowCoordinates = signal.ShowCoordinates
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
                    ShowCoordinates = sw.ShowCoordinates
                };
            }
            if (el is TrackCircuitBorderViewModel border)
            {
                return new TrackCircuitBorderViewModel
                {
                    Id = border.Id,
                    Name = border.Name,
                    Code = border.Code,
                    Description = border.Description,
                    X = border.X,
                    Y = border.Y,
                    Pos = border.Pos,
                    RelatedTrackId = border.RelatedTrackId,
                    ShowCoordinates = border.ShowCoordinates
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
                 nr.ShowCoordinates = r.ShowCoordinates;
                return nr;
            }
            if (el is AreaViewModel a)
            {
                var na = new AreaViewModel
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Type = a.Type
                };
                foreach (var b in a.Borders)
                {
                    // Find the border in new elements or assume it's already there
                    // This is tricky during clone for undo/redo.
                    // Usually we search by ID in the context of the snapshot.
                    na.Borders.Add(b); 
                }
                return na;
            }
            return null;
        }

        public List<BaseElementViewModel> TakeSnapshot()
        {
            return Elements.Select(el => CloneElement(el)).ToList();
        }

        public void AddHistory(List<BaseElementViewModel> oldState)
        {
            var newState = TakeSnapshot();
            History.Execute(new StateSnapshotAction(oldState, newState, RestoreState));
        }

        public void RestoreState(List<BaseElementViewModel> state)
        {
            // Capture selected IDs to restore selection if possible
            var selectedIds = SelectedElements.Select(el => el.Id).ToList();

            Elements.CollectionChanged -= Elements_CollectionChanged;
            Elements.Clear();
            foreach (var cat in ActiveInfrastructure.Categories) cat.Items.Clear();
            SelectedElements.Clear();

            foreach (var el in state)
            {
                var clone = CloneElement(el);
                Elements.Add(clone);
                if (selectedIds.Contains(clone.Id))
                {
                    clone.IsSelected = true; // This will trigger Element_PropertyChanged and add to SelectedElements
                }
            }
            Elements.CollectionChanged += Elements_CollectionChanged;
            UpdateSelectionState();
        }

        private void InitializeInfrastructure()
        {
            ActiveInfrastructure = new InfrastructureViewModel
            {
                Id = "inf001",
                Name = "Default Infrastructure"
            };
            ActiveInfrastructure.Categories.Add(new CategoryViewModel { Title = "Route" });
            ActiveInfrastructure.Categories.Add(new CategoryViewModel { Title = "Area" });
            ActiveInfrastructure.Categories.Add(new CategoryViewModel { Title = "Track" });
            ActiveInfrastructure.Categories.Add(new CategoryViewModel { Title = "Signal" });
            ActiveInfrastructure.Categories.Add(new CategoryViewModel { Title = "Point" });
            
            TreeRoots.Add(ActiveInfrastructure);
        }

        private void Elements_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
             if (e.Action == NotifyCollectionChangedAction.Reset)
             {
                 foreach(var cat in ActiveInfrastructure.Categories) cat.Items.Clear();
             }
             
             if (e.NewItems != null)
             {
                 foreach(BaseElementViewModel item in e.NewItems)
                 {
                     AddToCategory(item);
                     item.PropertyChanged += OnElementPropertyChanged;
                     UpdateTrackChildrenBinding(item);
                 }
             }
             if (e.OldItems != null)
             {
                 foreach(BaseElementViewModel item in e.OldItems)
                 {
                     RemoveFromCategory(item);
                     item.PropertyChanged -= OnElementPropertyChanged;
                     RemoveFromTrackChildren(item);
                 }
             }
        }

        private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SignalViewModel || sender is TrackCircuitBorderViewModel)
            {
                if (e.PropertyName == nameof(SignalViewModel.RelatedTrackId))
                {
                    UpdateTrackChildrenBinding(sender as BaseElementViewModel);
                }
            }
            else if (sender is TrackViewModel track)
            {
                 // Check geometry changes
                 if (e.PropertyName == nameof(TrackViewModel.X) || e.PropertyName == nameof(TrackViewModel.Y) || 
                     e.PropertyName == nameof(TrackViewModel.X2) || e.PropertyName == nameof(TrackViewModel.Y2) ||
                     e.PropertyName == nameof(CurvedTrackViewModel.MX) || e.PropertyName == nameof(CurvedTrackViewModel.MY))
                 {
                     CheckConnections(track);
                 }
            }
        }

        private void CheckConnections(TrackViewModel source)
        {
             // Check Begin Node (X, Y)
             CheckNodeConnection(source, source.BeginNode, source.X, source.Y);

             // Check End Node (X2, Y2)
             CheckNodeConnection(source, source.EndNode, source.X2, source.Y2);
        }

        private void CheckNodeConnection(TrackViewModel sourceTrack, TrackNodeViewModel sourceNode, double x, double y)
        {
             double tolerance = 1.0;
             TrackNodeViewModel? nearestNode = null;
             TrackViewModel? nearestTrack = null;
             double minDist = double.MaxValue;

             foreach(var other in Elements.OfType<TrackViewModel>())
             {
                  if (other == sourceTrack) continue;
                  
                  // Check other Begin
                  double d1 = Math.Sqrt(Math.Pow(other.X - x, 2) + Math.Pow(other.Y - y, 2));
                  if (d1 < minDist && d1 < tolerance)
                  {
                      minDist = d1;
                      nearestTrack = other;
                      nearestNode = other.BeginNode;
                  }

                  // Check other End
                  double d2 = Math.Sqrt(Math.Pow(other.X2 - x, 2) + Math.Pow(other.Y2 - y, 2));
                  if (d2 < minDist && d2 < tolerance)
                  {
                      minDist = d2;
                      nearestTrack = other;
                      nearestNode = other.EndNode;
                  }
             }

             if (nearestTrack != null && nearestNode != null)
             {
                  // Connect
                  sourceNode.NodeType = TrackNodeType.Connection;
                  sourceNode.ConnectedTrackId = nearestTrack.Id;
                  sourceNode.ConnectedNodeId = nearestNode.Id; // Ref to node ID or Role? Usually just Track Ref is enough, but user asked for Conn. ID and Ref.
                  // Assuming Node ID is enough. 
                  
                  // Bidirectional? User requirement implies automatic update.
                  // If source connects to dest, dest should connect to source.
                  nearestNode.NodeType = TrackNodeType.Connection;
                  nearestNode.ConnectedTrackId = sourceTrack.Id;
                  nearestNode.ConnectedNodeId = sourceNode.Id;
             }
             else
             {
                  // Disconnect if it WAS a connection (and now no overlap)
                  // User said: "if disconnected... auto change to None"
                  // But only if it WAS a Connection to avoid overwriting BufferStop/OpenEnd manually set?
                  // Logic: If it IS a Connection, and no overlap -> revert to None.
                  // If it is NOT a Connection (e.g. BufferStop), and no overlap -> do nothing.
                  // Wait, if I move a BufferStop into overlap, it should become Connection? Yes.
                  
                  if (sourceNode.NodeType == TrackNodeType.Connection)
                  {
                      sourceNode.NodeType = TrackNodeType.None;
                      sourceNode.ConnectedTrackId = null;
                      sourceNode.ConnectedNodeId = null;
                  }
             }
        }

        private void RemoveFromTrackChildren(BaseElementViewModel item)
        {
            foreach (var track in Elements.OfType<TrackViewModel>())
            {
                if (track.Children.Contains(item))
                {
                    track.Children.Remove(item);
                }
            }
        }

        private void UpdateTrackChildrenBinding(BaseElementViewModel item)
        {
            string? relatedId = null;
            if (item is SignalViewModel s) relatedId = s.RelatedTrackId;
            else if (item is TrackCircuitBorderViewModel b) relatedId = b.RelatedTrackId;

            if (relatedId == null)
            {
                RemoveFromTrackChildren(item);
                return;
            }

            var targetTrack = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == relatedId);
            
            if (targetTrack != null && targetTrack.Children.Contains(item)) return;

            RemoveFromTrackChildren(item);

            if (targetTrack != null)
            {
                targetTrack.Children.Add(item);
                if (item is TrackCircuitBorderViewModel border)
                {
                    border.Angle = Math.Atan2(targetTrack.Y2 - targetTrack.Y, targetTrack.X2 - targetTrack.X) * 180 / Math.PI;
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
             else if (item is AreaViewModel) catTitle = "Area";
             
              if (!string.IsNullOrEmpty(catTitle))
             {
                 var cat = ActiveInfrastructure.Categories.FirstOrDefault(c => c.Title == catTitle);
                 if (cat != null && !cat.Items.Contains(item))
                 {
                     cat.Items.Add(item);
                 }
             }
             else if (item is TrackCircuitBorderViewModel border)
             {
                 // Handle nested border
                 if (!string.IsNullOrEmpty(border.RelatedTrackId))
                 {
                     var track = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == border.RelatedTrackId);
                     if (track != null && !track.Children.Contains(border))
                     {
                         track.Children.Add(border);
                     }
                 }
             }
             else if (item is SignalViewModel signal)
             {
                 catTitle = "Signal";
                 if (!string.IsNullOrEmpty(signal.RelatedTrackId))
                 {
                     var track = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == signal.RelatedTrackId);
                     if (track != null && !track.Children.Contains(signal))
                     {
                         track.Children.Add(signal);
                     }
                 }
             }
             
             item.PropertyChanged += Element_PropertyChanged;
        }

        public void UpdateBorderParent(TrackCircuitBorderViewModel border)
        {
            if (string.IsNullOrEmpty(border.RelatedTrackId)) return;

            // Remove from ANY existing parent
            foreach(var t in Elements.OfType<TrackViewModel>())
            {
                if (t.Children.Contains(border)) t.Children.Remove(border);
            }

            // Add to new parent
            var track = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == border.RelatedTrackId);
            if (track != null)
            {
                if (!track.Children.Contains(border)) track.Children.Add(border);
            }
        }

        private void RemoveFromCategory(BaseElementViewModel item)
        {
             item.PropertyChanged -= Element_PropertyChanged;
             if (SelectedElements.Contains(item))
             {
                 SelectedElements.Remove(item);
                 UpdateSelectionState();
             }

             if (item is TrackCircuitBorderViewModel border)
             {
                 if (!string.IsNullOrEmpty(border.RelatedTrackId))
                 {
                     var track = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == border.RelatedTrackId);
                     track?.Children.Remove(border);
                 }
             }
             else
             {
                 foreach(var cat in ActiveInfrastructure.Categories)
                 {
                     if (cat.Items.Contains(item))
                     {
                         cat.Items.Remove(item);
                         break;
                     }
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
            else if (e.PropertyName == "RelatedTrackId")
            {
                if (sender is BaseElementViewModel vm)
                {
                    UpdateTrackChildrenBinding(vm);
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

private void UpdateTrackNodesToSwitch(SwitchViewModel sw)
{
    // If Scenario 1: One End (Entering) -> Multiple Begins (Principle/Diverging)
    // If Scenario 2: One Begin (Entering) -> Multiple Ends (Principle/Diverging)
    
    UpdateTrackNode(sw.EnteringTrackId, !sw.IsScenario1, sw.X, sw.Y);
    UpdateTrackNode(sw.PrincipleTrackId, sw.IsScenario1, sw.X, sw.Y);
    foreach (var divId in sw.DivergingTrackIds)
    {
        UpdateTrackNode(divId, sw.IsScenario1, sw.X, sw.Y);
    }
}

private void UpdateTrackNode(string? trackId, bool isBegin, double x, double y)
{
    if (string.IsNullOrEmpty(trackId)) return;
    var track = Elements.OfType<TrackViewModel>().FirstOrDefault(t => t.Id == trackId);
    if (track == null) return;

    if (isBegin)
    {
        // Don't trigger if already at the same position to avoid circular updates if we add back-sync later
        if (Math.Abs(track.X - x) < 0.001 && Math.Abs(track.Y - y) < 0.001) return;
        track.X = x;
        track.Y = y;
    }
    else
    {
        if (Math.Abs(track.X2 - x) < 0.001 && Math.Abs(track.Y2 - y) < 0.001) return;
        track.X2 = x;
        track.Y2 = y;
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

        public void ClearAllSelections()
        {
            var selected = SelectedElements.ToList();
            foreach (var el in selected)
            {
                el.IsSelected = false;
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
            var switchesToRemove = existingSwitches.Where(sw => !clusters.Any(c => Math.Sqrt(Math.Pow(sw.X - c.X, 2) + Math.Pow(sw.Y - c.Y, 2)) < 500.0)).ToList();

            foreach (var sw in switchesToRemove) Elements.Remove(sw);

            int maxId = 0;
            foreach (var sw in Elements.OfType<SwitchViewModel>())
            {
                if (sw.Id.StartsWith("P") && int.TryParse(sw.Id.Substring(1), out int num))
                    if (num > maxId) maxId = num;
            }

            foreach (var c in clusters)
            {
                var sw = Elements.OfType<SwitchViewModel>().FirstOrDefault(s => Math.Sqrt(Math.Pow(s.X - c.X, 2) + Math.Pow(s.Y - c.Y, 2)) < 500.0);
                if (sw == null)
                {
                    maxId++;
                    sw = new SwitchViewModel
                    {
                        Id = $"sw{maxId}",
                        Name = $"sw{maxId}",
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
                    // Existing switch, keep its current position (respect manual placement)
                }
            }
        }

        public void CreateAreaFromSelectedBorders()
        {
            var selectedBorders = SelectedElements.OfType<TrackCircuitBorderViewModel>().ToList();
            if (selectedBorders.Count == 0) return;

            var oldState = TakeSnapshot();

            var area = new AreaViewModel
            {
                Id = GetNextId("ar"),
                Name = "New Area",
                Type = "trackSection"
            };

            foreach (var b in selectedBorders)
            {
                area.Borders.Add(b);
            }

            Elements.Add(area);
            AddHistory(oldState);

            // Select the new area
            foreach (var el in Elements) el.IsSelected = (el == area);
            SelectedElement = area;
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
            return $"{prefix}{max + 1}";
        }
    }
}
