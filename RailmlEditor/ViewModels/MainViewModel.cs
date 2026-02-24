using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using RailmlEditor.Models;
using RailmlEditor.Utils;
using RailmlEditor.ViewModels.Elements;
using System.IO;
using System.Text.Json;

namespace RailmlEditor.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<BaseElementViewModel> Elements { get; } = new ObservableCollection<BaseElementViewModel>();
        public ObservableCollection<BaseElementViewModel> SelectedElements { get; } = new ObservableCollection<BaseElementViewModel>();

        public ObservableCollection<InfrastructureViewModel> TreeRoots { get; } = new ObservableCollection<InfrastructureViewModel>();
        
        private InfrastructureViewModel _activeInfrastructure = null!;
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

        public bool SuppressTopologyUpdates { get; set; } = false;

        public event Action<SwitchBranchInfo>? PrincipleTrackSelectionRequested;

        public ICommand SelectCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand OpenTemplatesCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        private List<BaseElementViewModel> _clipboard = new List<BaseElementViewModel>();

        private bool _isEditMode = false;
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (PasteCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public UndoRedoManager History { get; } = new();

        private string? _currentFilePath;
        public string? CurrentFilePath
        {
            get => _currentFilePath;
            set => SetProperty(ref _currentFilePath, value);
        }

        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand SaveAsProjectCommand { get; }
        public ICommand CreateAreaCommand { get; }

        public event Action? RequestUpdateTitle;

        private readonly RailmlEditor.Logic.TopologyManager _topologyManager = new RailmlEditor.Logic.TopologyManager();

        public MainViewModel()
        {
            InitializeInfrastructure();
            Elements.CollectionChanged += Elements_CollectionChanged;
            _topologyManager.PrincipleTrackSelectionRequested += info => PrincipleTrackSelectionRequested?.Invoke(info);

            History.StateChanged += (s, e) => { 
                (UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            UndoCommand = new RelayCommand(_ => History.Undo(), _ => History.CanUndo && IsEditMode);
            RedoCommand = new RelayCommand(_ => History.Redo(), _ => History.CanRedo && IsEditMode);

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

            DeleteCommand = new RelayCommand(_ => DeleteSelected(), _ => IsEditMode);
            CopyCommand = new RelayCommand(_ => CopySelected()); // Copy is allowed in read-only
            PasteCommand = new RelayCommand(_ => PasteElements(), _ => IsEditMode);

            OpenTemplatesCommand = new RelayCommand(_ => 
            {
                var win = new TemplatesWindow(this);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            });

            NewProjectCommand = new RelayCommand(_ => NewProject());
            OpenProjectCommand = new RelayCommand(_ => OpenProject());
            SaveProjectCommand = new RelayCommand(_ => SaveProject());
            SaveAsProjectCommand = new RelayCommand(_ => SaveAsProject());
            CreateAreaCommand = new RelayCommand(_ => CreateAreaFromSelectedBorders());

            // Test Data
            // Test Data Removed

            LoadCustomTemplates();
        }


        public void AddElement(string type, System.Windows.Point position)
        {
            var oldState = TakeSnapshot();
            BaseElementViewModel? newElement = null;

            if (type == "Track")
            {
                newElement = new TrackViewModel
                {
                    Id = GetNextId("tr"),
                    X = position.X, 
                    Y = position.Y,
                    Length = 100 
                };
            }
            else if (type == "Switch")
            {
                newElement = new SwitchViewModel
                {
                    Id = GetNextId("sw"),
                    X = position.X,
                    Y = position.Y
                };
            }
            else if (type == "Signal")
            {
                newElement = new SignalViewModel
                {
                    Id = GetNextId("sig"),
                    X = position.X,
                    Y = position.Y
                };
            }
            else if (type == "Corner")
            {
                double mx = position.X + 20;
                double my = position.Y - 40;
                newElement = new CurvedTrackViewModel
                {
                    Id = GetNextId("tr"),
                    Code = "corner",
                    X = position.X,
                    Y = position.Y,
                    MX = mx,
                    MY = my,
                    X2 = mx + 10,
                    Y2 = my
                };
            }
            else if (type == "Single") AddDoubleTrack("single.railml", position);
            else if (type == "SingleR") AddDoubleTrack("singleR.railml", position);
            else if (type == "SingleU") AddDoubleTrack("singleU.railml", position);
            else if (type == "SingleRU") AddDoubleTrack("singleRU.railml", position);
            else if (type == "Route") newElement = new RouteViewModel { Id = GetNextId("R") };
            else if (type == "Double") AddDoubleTrack("double.railml", position);
            else if (type == "DoubleR") AddDoubleTrack("doubleR.railml", position);
            else if (type == "Cross") AddDoubleTrack("cross.railml", position);
            
            if (newElement != null)
            {
                Elements.Add(newElement);
                if (newElement is TrackViewModel) UpdateProximitySwitches();
                AddHistory(oldState);
            }
        }

        private void NewProject()
        {
            if (Elements.Count > 0)
            {
                var result = MessageBox.Show("Save changes before creating new project?", "New Project", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes) SaveProject();
            }
            Elements.Clear();
            CurrentFilePath = null;
            RequestUpdateTitle?.Invoke();
        }

        private void OpenProject()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "RailML Files (*.xml;*.railml)|*.xml;*.railml|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                CurrentFilePath = dialog.FileName;
                var service = new Services.RailmlService();
                try
                {
                    service.Load(CurrentFilePath, this);
                    RequestUpdateTitle?.Invoke();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}");
                }
            }
        }

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(CurrentFilePath))
            {
                SaveAsProject();
                return;
            }

            var service = new Services.RailmlService();
            try
            {
                service.Save(CurrentFilePath, this);
                MessageBox.Show("File saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}");
            }
        }

        private void SaveAsProject()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "railway"; 
            dlg.DefaultExt = ".railml"; 
            dlg.Filter = "RailML documents (.railml)|*.railml"; 

            if (dlg.ShowDialog() == true)
            {
                CurrentFilePath = dlg.FileName;
                var service = new Services.RailmlService();
                try
                {
                    service.Save(CurrentFilePath, this);
                    RequestUpdateTitle?.Invoke();
                    MessageBox.Show("File saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }
        }

        private void DeleteSelected()
        {
            if (!IsEditMode) return;
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
                var c = CloneElement(el);
                if (c != null) _clipboard.Add(c);
            }
        }

        private void PasteElements()
        {
            if (!IsEditMode) return;
            if (!_clipboard.Any()) return;
            var oldState = TakeSnapshot();

            // Clear current selection to select newly pasted items
            foreach (var el in Elements) el.IsSelected = false;

            foreach (var el in _clipboard)
            {
                var clone = CloneElement(el);
                if (clone == null) continue;
                
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

                Elements.Add(clone);
                clone.IsSelected = true;
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
                        string? probeRoot = baseDir;
                        for (int i = 0; i < 5; i++)
                        {
                            if (probeRoot == null) break;
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
                    Elements.Add(el);
                    el.IsSelected = true;
                }
                UpdateSelectionState();
                AddHistory(oldState);
            }
        }

        private BaseElementViewModel? CloneElement(BaseElementViewModel el)
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
                    nr.SwitchAndPositions.Add(new SwitchPositionViewModel { SwitchRef = s.SwitchRef, SwitchPosition = s.SwitchPosition, RemoveCommand = new RelayCommand(p => { if (p is SwitchPositionViewModel vm) nr.SwitchAndPositions.Remove(vm); }) });
                foreach (var s in r.OverlapSwitchAndPositions)
                    nr.OverlapSwitchAndPositions.Add(new SwitchPositionViewModel { SwitchRef = s.SwitchRef, SwitchPosition = s.SwitchPosition, RemoveCommand = new RelayCommand(p => { if (p is SwitchPositionViewModel vm) nr.OverlapSwitchAndPositions.Remove(vm); }) });
                foreach (var s in r.ReleaseSections)
                    nr.ReleaseSections.Add(new ReleaseSectionViewModel { TrackRef = s.TrackRef, FlankProtection = s.FlankProtection, RemoveCommand = new RelayCommand(p => { if (p is ReleaseSectionViewModel vm) nr.ReleaseSections.Remove(vm); }) });
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
            return Elements.Select(el => CloneElement(el)).Where(e => e != null).Cast<BaseElementViewModel>().ToList();
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
                if (clone != null)
                {
                    Elements.Add(clone);
                    if (selectedIds.Contains(clone.Id))
                    {
                        clone.IsSelected = true; // This will trigger Element_PropertyChanged and add to SelectedElements
                    }
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

        private void Elements_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
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
                    if (sender is BaseElementViewModel vm) UpdateTrackChildrenBinding(vm);
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
            else if (sender is SwitchViewModel sw)
            {
                // We no longer snap tracks to the switch ptag position in real-time.
                // The ptag (X,Y) can move independently.
                // Topology is maintained via track IDs and proximity clustering.
            }
        }

        private void CheckConnections(TrackViewModel source)
        {
             _topologyManager.CheckConnections(source, Elements);
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

        private void Element_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            _topologyManager.UpdateTrackNodesToSwitch(sw, Elements);
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
            _topologyManager.UpdateProximitySwitches(Elements);
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

        private readonly RailmlEditor.Logic.IdGenerator _idGenerator = new RailmlEditor.Logic.IdGenerator();

        public string GetNextId(string prefix)
        {
            return _idGenerator.GetNextId(Elements, prefix);
        }
    }
}

