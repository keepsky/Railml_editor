using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
        public double X2
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
        public double Y2
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
            return null;
        }

        private void InitializeTreeCategories()
        {
            TreeCategories.Add(new CategoryViewModel { Title = "Track" });
            TreeCategories.Add(new CategoryViewModel { Title = "Signal" });
            TreeCategories.Add(new CategoryViewModel { Title = "Point" });
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
             
             var cat = TreeCategories.FirstOrDefault(c => c.Title == catTitle);
             cat?.Items.Add(item);
             
             item.PropertyChanged += Element_PropertyChanged;
        }

        private void RemoveFromCategory(BaseElementViewModel item)
        {
             item.PropertyChanged -= Element_PropertyChanged;
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

                    // Update primary SelectedElement or BulkEdit for Property Grid
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
        public void UpdateProximitySwitches()
        {
            // 1. Collect all endpoints
            var tracks = Elements.OfType<TrackViewModel>().ToList();
            var points = new System.Collections.Generic.List<(double X, double Y, string TrackId)>();

            foreach (var t in tracks)
            {
                points.Add((t.X, t.Y, t.Id));
                points.Add((t.X2, t.Y2, t.Id));
            }

            // 2. Cluster points (Simple greedy clustering or just iterate)
            // We want unique locations where count > 2.
            var clusters = new System.Collections.Generic.List<(double X, double Y, int Count)>();
            var processedIndices = new System.Collections.Generic.HashSet<int>();

            for (int i = 0; i < points.Count; i++)
            {
                if (processedIndices.Contains(i)) continue;

                double sumX = points[i].X;
                double sumY = points[i].Y;
                int count = 1;
                processedIndices.Add(i);

                // Find neighbors
                for (int j = i + 1; j < points.Count; j++)
                {
                    if (processedIndices.Contains(j)) continue;

                    double dist = System.Math.Sqrt(System.Math.Pow(points[i].X - points[j].X, 2) + System.Math.Pow(points[i].Y - points[j].Y, 2));
                    if (dist < 5.0) // Tolerance
                    {
                        sumX += points[j].X;
                        sumY += points[j].Y;
                        count++;
                        processedIndices.Add(j);
                    }
                }

                if (count > 2)
                {
                    clusters.Add((sumX / count, sumY / count, count));
                }
            }

            // 3. Reconcile Switches
            var existingSwitches = Elements.OfType<SwitchViewModel>().ToList();
            var switchesToRemove = new System.Collections.Generic.List<SwitchViewModel>();

            // Check existing switches validity
            foreach (var sw in existingSwitches)
            {
                bool isValid = false;
                foreach (var c in clusters)
                {
                    double dist = System.Math.Sqrt(System.Math.Pow(sw.X - c.X, 2) + System.Math.Pow(sw.Y - c.Y, 2));
                    if (dist < 10.0)
                    {
                        isValid = true;
                        break;
                    }
                }
                if (!isValid) switchesToRemove.Add(sw);
            }

            foreach (var sw in switchesToRemove)
            {
                Elements.Remove(sw);
            }

            // Determine Next ID based on existing Switches only
            int maxId = 0;
            foreach (var sw in Elements.OfType<SwitchViewModel>())
            {
                if (sw.Id.StartsWith("P") && int.TryParse(sw.Id.Substring(1), out int num))
                {
                     if (num > maxId) maxId = num;
                }
            }

            // Check clusters for missing switches
            foreach (var c in clusters)
            {
                if (!Elements.OfType<SwitchViewModel>().Any(sw => System.Math.Sqrt(System.Math.Pow(sw.X - c.X, 2) + System.Math.Pow(sw.Y - c.Y, 2)) < 10.0))
                {
                    maxId++;
                    var newSwitch = new SwitchViewModel
                    {
                        Id = $"P{maxId:D3}",
                        Name = $"P{maxId:D3}", // Auto-assign Name as ID for visibility
                        X = c.X,
                        Y = c.Y
                    };
                    Elements.Add(newSwitch);
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
