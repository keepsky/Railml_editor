using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using RailmlEditor.Models;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels.Elements
{
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
                    string num = System.Text.RegularExpressions.Regex.Match(Id, @"\d+").Value;
                    BeginNode.Id = "tb" + num;
                    EndNode.Id = "te" + num;
                    BeginNode.ConnectionId = "cb" + num;
                    EndNode.ConnectionId = "ce" + num;
                }
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
                
                if (!string.IsNullOrEmpty(Id))
                {
                    string num = System.Text.RegularExpressions.Regex.Match(Id, @"\d+").Value;
                    if (sender == BeginNode) BeginNode.ConnectionId = "cb" + num;
                    else if (sender == EndNode) EndNode.ConnectionId = "ce" + num;
                }
            }
            if (e.PropertyName == nameof(TrackNodeViewModel.ConnectedNodeId) || e.PropertyName == nameof(TrackNodeViewModel.ConnectedTrackId))
            {
                var node = sender as TrackNodeViewModel;
                if (node != null)
                {
                    node.ConnectionRef = node.ConnectedNodeId;
                }
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

        public virtual double Length
        {
            get => System.Math.Sqrt(System.Math.Pow(X2 - X, 2) + System.Math.Pow(Y2 - Y, 2));
            set
            {
                X2 = X + value;
                Y2 = Y;
                OnPropertyChanged(nameof(Length));
            }
        }

        public ObservableCollection<BaseElementViewModel> Children { get; } = new();

        public TrackNodeViewModel BeginNode { get; } = new TrackNodeViewModel();
        public TrackNodeViewModel EndNode { get; } = new TrackNodeViewModel();

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

        private void OnBeginTypeChanged()
        {
            BeginNode.NodeType = BeginType;
            BeginNode.Role = "Begin";
            
            if (!Children.Contains(BeginNode)) Children.Insert(0, BeginNode); 
        }

        private void OnEndTypeChanged()
        {
            EndNode.NodeType = EndType;
            EndNode.Role = "End";

            if (!Children.Contains(EndNode)) Children.Add(EndNode);
        }

        private string GenerateId(string prefix)
        {
            var match = System.Text.RegularExpressions.Regex.Match(Id ?? "", @"\d+$");
            string number = match.Success ? match.Value : "1";
            return $"{prefix}{number}";
        }

        public IEnumerable<TrackNodeType> AvailableTrackNodeTypes => System.Enum.GetValues(typeof(TrackNodeType)).Cast<TrackNodeType>();
    }




}


