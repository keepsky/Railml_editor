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

        private string _name;
        public string Name
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
        
        public override double X
        {
            get => base.X;
            set
            {
                if (base.X != value)
                {
                    base.X = value;
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
        // Orientation, BranchAngle etc.
    }

    public class SignalViewModel : BaseElementViewModel
    {
        public override string TypeName => "Signal";
        
        private string _relatedTrackId;
        public string RelatedTrackId
        {
            get => _relatedTrackId;
            set => SetProperty(ref _relatedTrackId, value);
        }

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
        public ObservableCollection<CategoryViewModel> TreeCategories { get; } = new ObservableCollection<CategoryViewModel>();

        private BaseElementViewModel _selectedElement;
        public BaseElementViewModel SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        public ICommand SelectCommand { get; }

        public MainViewModel()
        {
            InitializeTreeCategories();
            Elements.CollectionChanged += Elements_CollectionChanged;

            SelectCommand = new RelayCommand(param => 
            {
                if (param is BaseElementViewModel element)
                {
                    SelectedElement = element;
                }
            });

            // Test Data
            Elements.Add(new TrackViewModel { Id = "Tr01", Name = "Track 1", X = 100, Y = 100, Length = 200 });
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
                if (sender is BaseElementViewModel vm && vm.IsSelected)
                {
                    SelectedElement = vm;
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

            // Check clusters for missing switches
            foreach (var c in clusters)
            {
                if (!Elements.OfType<SwitchViewModel>().Any(sw => System.Math.Sqrt(System.Math.Pow(sw.X - c.X, 2) + System.Math.Pow(sw.Y - c.Y, 2)) < 10.0))
                {
                    var newSwitch = new SwitchViewModel
                    {
                        Id = $"Sw{Elements.Count + 1}",
                        X = c.X,
                        Y = c.Y
                    };
                    Elements.Add(newSwitch);
                }
            }
        }
    }
}
