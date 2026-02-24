
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using System.Windows.Media;

namespace RailmlEditor
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _viewModel.PrincipleTrackSelectionRequested += OnPrincipleTrackSelectionRequested;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.DataContext = _viewModel;
            _viewModel.RequestUpdateTitle += UpdateTitle;
            this.Closing += MainWindow_Closing;
            
            
            // Set Initial Theme based on Config
            // ConfigService.Load() is called in App.OnStartup, but MainWindow might be created before that if StartupUri is used?
            // Let's ensure Load() is called or just access Current which defaults to Light.
            // Actually, safe to just read Current.Theme
            Services.ConfigService.Load(); // Ensure it's loaded

            dockManager.ActiveContentChanged += DockManager_ActiveContentChanged;

            SetTheme(Services.ConfigService.Current.Theme);
            UpdateTitle(); // Initialize Title
        }

        private void DockManager_ActiveContentChanged(object? sender, EventArgs e)
        {
            _viewModel.ActiveDocument = dockManager.ActiveContent as DocumentViewModel;
        }

        private void UpdateTitle()
        {
            string fileName = "notitle.railml";
            if (_viewModel.ActiveDocument != null && !string.IsNullOrEmpty(_viewModel.ActiveDocument.FilePath))
            {
                fileName = System.IO.Path.GetFileName(_viewModel.ActiveDocument.FilePath);
            }
            string dirtyMark = (_viewModel.ActiveDocument?.IsDirty == true) ? "*" : "";
            this.Title = $"RailML Editor - {fileName}{dirtyMark}";
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var doc in _viewModel.Documents.ToList())
            {
                if (doc.IsDirty)
                {
                    var result = MessageBox.Show($"Save changes to {doc.Title.TrimEnd('*')} before closing?", "Exit Application", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Cancel)
                    {
                        e.Cancel = true; // Stop closing
                        return;
                    }
                    else if (result == MessageBoxResult.Yes)
                    {
                        if (!_viewModel.SaveDocument(doc)) 
                        {
                            e.Cancel = true; // Save failed or cancelled
                            return;
                        }
                    }
                }
            }
        }

        public void SetTheme(string theme)
        {
             if (theme == "Light")
             {
                 dockManager.Theme = new AvalonDock.Themes.Vs2013LightTheme();
             }
             else
             {
                 dockManager.Theme = new AvalonDock.Themes.Vs2013DarkTheme();
             }
        }

        private Point _toolboxDragStart;
        private Point _startPoint;
        private bool _isDragging;
        private FrameworkElement? _draggedControl;
        private System.Collections.Generic.Dictionary<BaseElementViewModel, Point> _originalPositions = new System.Collections.Generic.Dictionary<BaseElementViewModel, Point>();
        private System.Collections.Generic.List<BaseElementViewModel>? _beforeDragSnapshot;

        private bool _isPlacingBorder = false;
        private TrackCircuitBorderViewModel? _ghostBorder;

        private void Toolbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _toolboxDragStart = e.GetPosition(null);
        }

        private void Toolbox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is Button button)
            {
                Point currentPos = e.GetPosition(null);
                if (Math.Abs(currentPos.X - _toolboxDragStart.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(currentPos.Y - _toolboxDragStart.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    string? type = button.Tag?.ToString();
                    DragDrop.DoDragDrop(button, type, DragDropEffects.Copy);
                }
            }
        }
        private void Toolbox_Click(object sender, RoutedEventArgs e)
        {
            if (!_viewModel.IsEditMode) return;
            if (sender is Button button)
            {
                string? type = button.Tag?.ToString();
                
                if (type == "Border")
                {
                    var elems = GetEditorElements(dockManager); // Fallback conceptually to ActiveContent's layout root.
                    // Actually, Toolbox_Click sender is a Button in the ToolBarTray.
                    // It is NOT inside the DataTemplate. So GetEditorElements(sender) will return null.
                    // We need to find the active document's MainGrid. Let's use dockManager to find it.
                    // Or, simpler, we could use the ActiveContent of DockingManager if we could.
                    
                    // The correct way in AvalonDock to find elements in the active document's view:
                    // Since StartBorderPlacement requires EditorElements, we must get it from the ActiveContent view.
                    // Best way: find the active View/Control. For now, since StartBorderPlacement modifies VM and cursor,
                    // we can find the MainGrid globally or skip cursor modify if not found.
                    
                    // Easy fix: just refactor StartBorderPlacement to not need EditorElements, 
                    // it only needed it for setting the Cursor on MainDesigner.
                    // Let's change StartBorderPlacement to set cursor globally or remove cursor logic.
                    // Or, pass null for now and handle it inside. Let's look at StartBorderPlacement.
                    
                    StartBorderPlacement(null!);
                }
                else if (type != null)
                {
                    _viewModel.AddElement(type, new Point(100, 100));
                }
            }
        }

        private void FileSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsView = new Views.SettingsView();
            settingsView.Owner = this;
            settingsView.ShowDialog();
        }

                private void MainDesigner_Drop(object sender, DragEventArgs e)
        {
            if (!_viewModel.IsEditMode) return;
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string? type = e.Data.GetData(DataFormats.StringFormat) as string;
                Point dropPosition = e.GetPosition(elems.MainDesigner);

                if (type == "Border")
                {
                    // Border drop not supported via generic AddElement yet, original code didn't handle it in Drop either
                }
                else if (type != null)
                {
                    _viewModel.AddElement(type, dropPosition);
                }
            }
        }

        private string GetNextId(string prefix)
        {
            return _viewModel.GetNextId(prefix);
        }



        // UI Element Retrieval Helper
        private class EditorElements
        {
            public Grid MainGrid { get; set; } = null!;
            public ItemsControl MainDesigner { get; set; } = null!;
            public Canvas OverlayCanvas { get; set; } = null!;
            public System.Windows.Shapes.Rectangle SelectionBox { get; set; } = null!;
            public ScrollViewer MainScrollViewer { get; set; } = null!;
            public ScaleTransform MainScaleTransform { get; set; } = null!;
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && typedChild.Name == name)
                {
                    return typedChild;
                }
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private EditorElements? GetEditorElements(object sender)
        {
            if (sender is DependencyObject dObj)
            {
                // Find the root MainGrid of this template instance
                Grid? mainGrid = sender as Grid;
                if (mainGrid == null || mainGrid.Name != "MainGrid")
                {
                    // If sender is not MainGrid, walk up to find it
                    DependencyObject current = dObj;
                    while (current != null)
                    {
                        if (current is Grid g && g.Name == "MainGrid")
                        {
                            mainGrid = g;
                            break;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }
                }

                if (mainGrid != null)
                {
                    var mainDesigner = FindVisualChild<ItemsControl>(mainGrid, "MainDesigner")!;
                    var scaleTransform = mainDesigner.LayoutTransform as ScaleTransform;
                    if (scaleTransform == null)
                    {
                        // Fallback or create if not found (though it should be defined in XAML)
                        scaleTransform = new ScaleTransform(1, 1);
                        mainDesigner.LayoutTransform = scaleTransform;
                    }

                    return new EditorElements
                    {
                        MainGrid = mainGrid,
                        MainDesigner = mainDesigner,
                        OverlayCanvas = FindVisualChild<Canvas>(mainGrid, "OverlayCanvas")!,
                        SelectionBox = FindVisualChild<System.Windows.Shapes.Rectangle>(mainGrid, "SelectionBox")!,
                        MainScrollViewer = FindVisualChild<ScrollViewer>(mainGrid, "MainScrollViewer")!,
                        MainScaleTransform = scaleTransform
                    };
                }
            }
            return null;
        }

        // Selection & Panning State
        private bool _isSelecting = false;
        private Point _selectionStartPoint;
        private Point _selectionStartLogicalPoint;
        private bool _isPanning = false;
        private Point _panStartPoint;

        // Thumb Drag State (Absolute)
        private Point _dragStartMousePos;
        private double _dragStartElementX;
        private double _dragStartElementY;
        private double _dragStartElementX2;
        private double _dragStartElementY2;
        private double _dragStartElementMX;
        private double _dragStartElementMY;

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured != null) return;
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (e.ChangedButton == MouseButton.Left)
            {
                // Start Selection
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(elems.OverlayCanvas);
                _selectionStartLogicalPoint = e.GetPosition(elems.MainDesigner);
                
                // Reset styling
                Canvas.SetLeft(elems.SelectionBox, _selectionStartPoint.X);
                Canvas.SetTop(elems.SelectionBox, _selectionStartPoint.Y);
                elems.SelectionBox.Width = 0;
                elems.SelectionBox.Height = 0;
                elems.SelectionBox.Visibility = Visibility.Visible;

                // Clear existing selection unless Ctrl or Shift is pressed
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    _viewModel.ClearAllSelections();
                    _viewModel.SelectedElement = null;
                }
                
                elems.MainDesigner.Focus();
                elems.MainDesigner.CaptureMouse();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                // Start Panning (Global Move)
                _isPanning = true;
                _panStartPoint = e.GetPosition(elems.MainGrid); // Initial point, acts as "Last Point"
                
                elems.MainDesigner.Focus();
                elems.MainDesigner.CaptureMouse();
                elems.MainDesigner.Cursor = Cursors.SizeAll;
                e.Handled = true;
            }
            
            if (_isPlacingBorder)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    FinalizeBorderPlacement(e.GetPosition(elems.MainDesigner), elems);
                    e.Handled = true;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    CancelBorderPlacement(elems);
                    e.Handled = true;
                }
            }
        }

        private void StartBorderPlacement(EditorElements? elems)
        {
            _isPlacingBorder = true;
            _ghostBorder = new TrackCircuitBorderViewModel
            {
                Id = _viewModel.GetNextId("td"),
                Name = "Border",
                X = -1000,
                Y = -1000
            };
            _viewModel.Elements.Add(_ghostBorder);
            
            if (elems != null)
            {
                elems.MainDesigner.Cursor = Cursors.Cross;
            }
            else
            {
                Mouse.OverrideCursor = Cursors.Cross;
            }
        }

        private void CancelBorderPlacement(EditorElements elems)
        {
            if (_ghostBorder != null) _viewModel.Elements.Remove(_ghostBorder);
            _isPlacingBorder = false;
            _ghostBorder = null;
            if (elems != null) elems.MainDesigner.Cursor = Cursors.Arrow;
            Mouse.OverrideCursor = null;
        }

        private void FinalizeBorderPlacement(Point pos, EditorElements elems)
        {
            if (_ghostBorder == null) return;

            TrackViewModel? nearestTrack = null;
            double minDist = 20; // Snap distance
            Point snapPoint = pos;
            double snapAngle = 0;

            foreach (var t in _viewModel.Elements.OfType<TrackViewModel>())
            {
                RailmlEditor.Utils.GeometryUtils.GetNearestPointOnTrack(pos, t, out Point nearest, out double dist, out double angle);
                if (dist < minDist)
                {
                    nearestTrack = t;
                    minDist = dist;
                    snapPoint = nearest;
                    snapAngle = angle;
                }
            }

            if (nearestTrack != null)
            {
                var oldState = _viewModel.TakeSnapshot();
                _ghostBorder.X = snapPoint.X;
                _ghostBorder.Y = snapPoint.Y;
                _ghostBorder.Angle = snapAngle;
                _ghostBorder.RelatedTrackId = nearestTrack.Id;
                
                double dx = _ghostBorder.X - nearestTrack.X;
                double dy = _ghostBorder.Y - nearestTrack.Y;
                _ghostBorder.Pos = Math.Round(Math.Sqrt(dx * dx + dy * dy)); 
            
                var finalBorder = _ghostBorder;
                _ghostBorder = null;
                _isPlacingBorder = false;
                elems.MainDesigner.Cursor = Cursors.Arrow;
                
                _viewModel.AddHistory(oldState);
                
                // Force update explorer parent
                _viewModel.UpdateBorderParent(finalBorder);
            }
            else
            {
                // Just place it if no track? User said "임의의 track위에 ... 클릭하면 ... 바인딩"
                // If not on track, maybe do nothing or just cancel?
                // Let's just keep it following mouse until a track is clicked or right-click to cancel.
            }
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (_isSelecting)
            {
                var curPos = e.GetPosition(elems.OverlayCanvas);
                var x = Math.Min(curPos.X, _selectionStartPoint.X);
                var y = Math.Min(curPos.Y, _selectionStartPoint.Y);
                var w = Math.Abs(curPos.X - _selectionStartPoint.X);
                var h = Math.Abs(curPos.Y - _selectionStartPoint.Y);

                Canvas.SetLeft(elems.SelectionBox, x);
                Canvas.SetTop(elems.SelectionBox, y);
                elems.SelectionBox.Width = w;
                elems.SelectionBox.Height = h;
            }
            else if (_isPlacingBorder && _ghostBorder != null)
            {
                var pos = e.GetPosition(elems.MainDesigner);
                _ghostBorder.X = pos.X;
                _ghostBorder.Y = pos.Y;
                _ghostBorder.Angle = 0;
                
                // Snapping preview
                foreach (var t in _viewModel.Elements.OfType<TrackViewModel>())
                {
                    RailmlEditor.Utils.GeometryUtils.GetNearestPointOnTrack(pos, t, out Point nearest, out double dist, out double angle);
                    if (dist < 20)
                    {
                        _ghostBorder.X = nearest.X;
                        _ghostBorder.Y = nearest.Y;
                        _ghostBorder.Angle = angle;
                        break;
                    }
                }
            }
            else if (_isPanning)
            {
                var curPos = e.GetPosition(elems.MainGrid);
                // Divide screen delta by current scale to get logical delta
                var deltaX = (curPos.X - _panStartPoint.X) / elems.MainScaleTransform.ScaleX;
                var deltaY = (curPos.Y - _panStartPoint.Y) / elems.MainScaleTransform.ScaleY;

                if (Math.Abs(deltaX) >= 1.0 || Math.Abs(deltaY) >= 1.0)
                {
                    double snapX = Math.Round(deltaX); // Snap to logical units? Or keep previous 10px snap?
                    // User might want to keep the 10px snap even when zoomed. 
                    // Let's stick to the 10px logical snap but make movement smooth relative to mouse.
                    
                    snapX = Math.Round(deltaX / 10.0) * 10.0;
                    double snapY = Math.Round(deltaY / 10.0) * 10.0;

                    if (snapX != 0 || snapY != 0)
                    {
                        foreach (var element in _viewModel.Elements)
                        {
                            // Skip bound elements as they will vary with Track
                            if (element is SignalViewModel s && !string.IsNullOrEmpty(s.RelatedTrackId)) continue;
                            if (element is TrackCircuitBorderViewModel b && !string.IsNullOrEmpty(b.RelatedTrackId)) continue;

                            element.X += snapX;
                            element.Y += snapY;

                            if (element is TrackViewModel track)
                            {
                                track.X2 += snapX;
                                track.Y2 += snapY;

                                if (track is CurvedTrackViewModel curved)
                                {
                                    curved.MX += snapX;
                                    curved.MY += snapY;
                                }
                            }
                        }
                        // Update "Last Point" by the snapped amount to avoid drift
                        _panStartPoint.X += snapX;
                        _panStartPoint.Y += snapY;
                    }
                }
            }
        }

        private void MainGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (_isSelecting)
            {
                _isSelecting = false;
                elems.SelectionBox.Visibility = Visibility.Collapsed;
                elems.MainDesigner.ReleaseMouseCapture();

                // Perform Selection Logic
                Point logicalEnd = e.GetPosition(elems.MainDesigner);
                Rect selectionRect = new Rect(_selectionStartLogicalPoint, logicalEnd);

                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    _viewModel.ClearAllSelections();
                }

                foreach (var element in _viewModel.Elements)
                {
                    bool hit = false;
                    
                    // 1. Generic Anchor Check (10x10 centered at X, Y for general hit)
                    Rect anchorBounds = new Rect(element.X - 5, element.Y - 5, 10, 10);
                    if (selectionRect.IntersectsWith(anchorBounds))
                    {
                        hit = true;
                    }

                    // 2. Type-specific accurate bounds
                    if (!hit && element is TrackViewModel track)
                    {
                        // Check end point
                        Rect endBounds = new Rect(track.X2 - 5, track.Y2 - 5, 10, 10);
                        if (selectionRect.IntersectsWith(endBounds))
                        {
                            hit = true;
                        }
                        
                        // Check line segment
                        if (!hit) hit = RailmlEditor.Utils.GeometryUtils.IntersectsLine(selectionRect, new Point(track.X, track.Y), new Point(track.X2, track.Y2));

                        if (track is CurvedTrackViewModel curved)
                        {
                            // Check midpoint
                            Rect midBounds = new Rect(curved.MX - 5, curved.MY - 5, 10, 10);
                            if (selectionRect.IntersectsWith(midBounds)) hit = true;
                            
                            // Check both segments
                            if (!hit)
                            {
                                hit = RailmlEditor.Utils.GeometryUtils.IntersectsLine(selectionRect, new Point(curved.X, curved.Y), new Point(curved.MX, curved.MY)) ||
                                      RailmlEditor.Utils.GeometryUtils.IntersectsLine(selectionRect, new Point(curved.MX, curved.MY), new Point(curved.X2, curved.Y2));
                            }
                        }
                    }
                    else if (!hit && element is SwitchViewModel sw)
                    {
                        // Tag bounds: (-15, 7) offset, Size 35x12
                        Rect tagBounds = new Rect(sw.X - 15.0, sw.Y + 7.0, 35, 12);
                        if (selectionRect.IntersectsWith(tagBounds)) hit = true;
                    }
                    else if (!hit && element is SignalViewModel signal)
                    {
                        // Signal grid is (0, 0, 20, 10) or (-20, 0, 20, 10) if flipped
                        Rect sigBounds = signal.IsFlipped 
                            ? new Rect(signal.X - 20, signal.Y, 20, 10) 
                            : new Rect(signal.X, signal.Y, 20, 10);
                        if (selectionRect.IntersectsWith(sigBounds)) hit = true;
                    }
                    else if (!hit && element is TrackCircuitBorderViewModel border)
                    {
                        // Border is 20x10 centered: (-10, -5, 20, 10)
                        Rect borderBounds = new Rect(border.X - 10, border.Y - 5, 20, 10);
                        if (selectionRect.IntersectsWith(borderBounds)) hit = true;
                    }

                    if (hit) element.IsSelected = true;
                }
            }
            else if (_isPanning)
            {
                _isPanning = false;
                elems.MainDesigner.ReleaseMouseCapture();
                elems.MainDesigner.Cursor = Cursors.Arrow;
            }
        }

        private void MainGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var selected = new System.Collections.Generic.List<BaseElementViewModel>();
                foreach (var el in _viewModel.Elements)
                {
                    if (el.IsSelected) selected.Add(el);
                }
                
                foreach (var el in selected)
                {
                    _viewModel.Elements.Remove(el);
                }
            }
        }

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsOrInsideThumb(e.OriginalSource as DependencyObject))
            {
                e.Handled = true; 
                return;
            }

            if (sender is FrameworkElement element && element.DataContext is BaseElementViewModel viewModel)
            {
                if (viewModel is SwitchViewModel)
                {
                    // Allow selection but prevent drag
                     if (e.ChangedButton == MouseButton.Left)
                    {
                        bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                        if (!viewModel.IsSelected && !isMulti)
                        {
                            _viewModel.ClearAllSelections();
                            viewModel.IsSelected = true;
                        }
                        else if (isMulti)
                        {
                             viewModel.IsSelected = !viewModel.IsSelected;
                        }
                        e.Handled = true;
                    }
                    return;
                }
                


                if (e.ChangedButton == MouseButton.Left)
                {
                    if (!_viewModel.IsEditMode)
                    {
                        // Allow selection only
                        _viewModel.SelectedElement = viewModel;
                         bool isMultiSelect = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                        if (!isMultiSelect)
                        {
                            _viewModel.ClearAllSelections();
                            viewModel.IsSelected = true;
                        }
                        else
                        {
                            viewModel.IsSelected = !viewModel.IsSelected;
                        }
                        e.Handled = true;
                        return;
                    }

                    _beforeDragSnapshot = _viewModel.TakeSnapshot();
                    _isDragging = true;
                    _draggedControl = element;
                    
                    var elems = GetEditorElements(sender);
                    if (elems != null)
                    {
                        _startPoint = e.GetPosition(elems.MainDesigner);
                    }
                    _viewModel.SuppressTopologyUpdates = true; // Added line
                    
                    bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                    // If not already selected and multi-select modifier not pressed, select only this.
                    if (!viewModel.IsSelected && !isMulti)
                    {
                        _viewModel.ClearAllSelections();
                        viewModel.IsSelected = true;
                    }
                    else if (isMulti)
                    {
                        viewModel.IsSelected = !viewModel.IsSelected;
                    }
                    
                    if (viewModel.IsSelected)
                    {
                        _originalPositions.Clear();
                        foreach (var el in _viewModel.Elements)
                        {
                            if (el.IsSelected)
                            {
                                 _originalPositions[el] = new Point(el.X, el.Y);
                            }
                        }
                    }

                    _viewModel.SelectedElement = viewModel; // Keep this for property grid focus
                    
                    elems?.MainDesigner.Focus();
                    element.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedControl != null)
            {
                var elems = GetEditorElements(sender);
                if (elems == null) return;
                
                Point currentPos = e.GetPosition(elems.MainDesigner);
                // Total Delta from Start
                double deltaX = currentPos.X - _startPoint.X;
                double deltaY = currentPos.Y - _startPoint.Y;

                // Snap delta to 10px grid if no constraints
                // But we want to snap individual elements relative to their original position or grid?
                // Previous logic: snap delta.
                
                double snapDeltaX = Math.Round(deltaX / 10.0) * 10.0;
                double snapDeltaY = Math.Round(deltaY / 10.0) * 10.0;

                foreach (var kvp in _originalPositions)
                {
                    var element = kvp.Key;
                    var orig = kvp.Value;
                    
                    // Proposed New Position based on Grid Snap Delta
                    double proposedX = orig.X + snapDeltaX;
                    double proposedY = orig.Y + snapDeltaY;
                    
                    // Calculate shift from current position (to propagate to X2/Y2)
                    double shiftX = proposedX - element.X;
                    double shiftY = proposedY - element.Y;
                    
                    // Update Position
                    element.X = proposedX;
                    element.Y = proposedY;

                    if (element is TrackViewModel trackVm)
                    {
                         trackVm.X2 += shiftX;
                         trackVm.Y2 += shiftY;
                         
                         if (trackVm is CurvedTrackViewModel curved)
                         {
                             curved.MX += shiftX;
                             curved.MY += shiftY;
                         }
                    }
                    else if (element is SwitchViewModel sw)
                    {
                        // X/Y already updated above
                    }
                }
                
                // Now apply Snapping Logic for Signals (Post-Processing)
                // This must run AFTER position updates to check if we fall into a snap well.
                foreach (var kvp in _originalPositions)
                {
                     var element = kvp.Key;
                     if (element is SignalViewModel signalVm)
                     {
                         // Temporary: Assume unbinding first.
                         var oldRelated = signalVm.RelatedTrackId;
                         signalVm.RelatedTrackId = null; 
                         
                         foreach (var other in _viewModel.Elements)
                         {
                             if (other is TrackViewModel t && other != signalVm)
                             {
                                 double distStartSq = Math.Pow(signalVm.X - t.X, 2) + Math.Pow(signalVm.Y - t.Y, 2);
                                 double distEndSq = Math.Pow(signalVm.X - t.X2, 2) + Math.Pow(signalVm.Y - t.Y2, 2);
                                 
                                 // Snap Radius 20px (400 sq)
                                 bool snapped = false;
                                 if (signalVm.Direction == "up" && distStartSq < 400.0)
                                 {
                                     signalVm.X = t.X;
                                     signalVm.Y = t.Y - 15;
                                     signalVm.RelatedTrackId = t.Id;
                                     signalVm.Pos = 0;
                                     snapped = true;
                                 }
                                 else if (signalVm.Direction == "down" && distEndSq < 400.0)
                                 {
                                     signalVm.X = t.X2;
                                     signalVm.Y = t.Y2 + 5;
                                     signalVm.RelatedTrackId = t.Id;
                                     signalVm.Pos = Math.Round(t.Length);
                                     snapped = true;
                                 }
                                 
                                 if (snapped) break;
                             }
                         }
                     }
                     else if (element is TrackCircuitBorderViewModel borderVm)
                     {
                          TrackViewModel? nearestTrack = null;
                          double minDist = 10; 
                          Point snapPos = new Point(borderVm.X, borderVm.Y);
                          double snapAngle = 0;

                          foreach (var t in _viewModel.Elements.OfType<TrackViewModel>())
                          {
                              RailmlEditor.Utils.GeometryUtils.GetNearestPointOnTrack(new Point(borderVm.X, borderVm.Y), t, out Point nearest, out double dist, out double angle);
                              if (dist < minDist)
                              {
                                  minDist = dist;
                                  nearestTrack = t;
                                  snapPos = nearest;
                                  snapAngle = angle;
                              }
                          }

                          if (nearestTrack != null)
                          {
                              borderVm.RelatedTrackId = nearestTrack.Id;
                              borderVm.X = snapPos.X;
                              borderVm.Y = snapPos.Y;
                              borderVm.Angle = snapAngle;
                              
                              double dx = borderVm.X - nearestTrack.X;
                              double dy = borderVm.Y - nearestTrack.Y;
                              borderVm.Pos = Math.Round(Math.Sqrt(dx * dx + dy * dy));
                          }
                          else
                          {
                              borderVm.RelatedTrackId = null;
                              borderVm.Angle = 0;
                              borderVm.Pos = 0;
                          }
                     }
                }
            }
        }



        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _beforeDragSnapshot = _viewModel.TakeSnapshot();
            var elems = GetEditorElements(sender);
            if (elems != null)
            {
                _dragStartMousePos = Mouse.GetPosition(elems.MainDesigner);
            }
            if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                _dragStartElementX = track.X;
                _dragStartElementY = track.Y;
                _dragStartElementX2 = track.X2;
                _dragStartElementY2 = track.Y2;
                if (track is CurvedTrackViewModel curved)
                {
                    _dragStartElementMX = curved.MX;
                    _dragStartElementMY = curved.MY;
                }
            }
        }

        private void StartThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                Point currentMousePos = Mouse.GetPosition(elems.MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                track.X = Math.Round((_dragStartElementX + totalDeltaX) / 10.0) * 10.0;
                track.Y = Math.Round((_dragStartElementY + totalDeltaY) / 10.0) * 10.0;
            }
        }

        private void EndThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             var elems = GetEditorElements(sender);
             if (elems == null) return;

             if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                Point currentMousePos = Mouse.GetPosition(elems.MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                track.X2 = Math.Round((_dragStartElementX2 + totalDeltaX) / 10.0) * 10.0;
                track.Y2 = Math.Round((_dragStartElementY2 + totalDeltaY) / 10.0) * 10.0;
            }
        }

        private void MidThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             var elems = GetEditorElements(sender);
             if (elems == null) return;

             if (sender is FrameworkElement thumb && thumb.DataContext is CurvedTrackViewModel curved)
             {
                Point currentMousePos = Mouse.GetPosition(elems.MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                curved.MX = Math.Round((_dragStartElementMX + totalDeltaX) / 10.0) * 10.0;
                curved.MY = Math.Round((_dragStartElementMY + totalDeltaY) / 10.0) * 10.0;
             }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             _viewModel.UpdateProximitySwitches();
             if (_beforeDragSnapshot != null) _viewModel.AddHistory(_beforeDragSnapshot);
        }

        private void OnPrincipleTrackSelectionRequested(SwitchBranchInfo info)
        {
            var selector = new JunctionPrincipleSelector(info.Candidates ?? new System.Collections.Generic.List<TrackViewModel>());
            selector.Owner = this;
            if (selector.ShowDialog() == true)
            {
                info.Callback?.Invoke(selector.SelectedTrack);
            }
            else
            {
                RollbackMove();
                info.Callback?.Invoke(null);
            }
        }

        private void RollbackMove()
        {
            foreach (var kvp in _originalPositions)
            {
                var element = kvp.Key;
                var orig = kvp.Value;
                
                double shiftX = orig.X - element.X;
                double shiftY = orig.Y - element.Y;

                element.X = orig.X;
                element.Y = orig.Y;

                if (element is TrackViewModel trackVm)
                {
                    trackVm.X2 += shiftX;
                    trackVm.Y2 += shiftY;
                    if (trackVm is CurvedTrackViewModel curved)
                    {
                        curved.MX += shiftX;
                        curved.MY += shiftY;
                    }
                }
            }
        }

        private void Item_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                if (_draggedControl != null)
                {
                    _draggedControl.ReleaseMouseCapture();
                    _draggedControl = null;
                }
                
                _viewModel.SuppressTopologyUpdates = false;
                _viewModel.UpdateProximitySwitches();
                if (_beforeDragSnapshot != null) _viewModel.AddHistory(_beforeDragSnapshot);
            }
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                double scaleFactor = e.Delta > 0 ? 0.1 : -0.1;
                
                // Get current scale
                double newScaleX = elems.MainScaleTransform.ScaleX + scaleFactor;
                double newScaleY = elems.MainScaleTransform.ScaleY + scaleFactor;

                // Clamp limits (0.2x to 5.0x)
                if (newScaleX < 0.2) newScaleX = 0.2;
                if (newScaleY < 0.2) newScaleY = 0.2;
                if (newScaleX > 5.0) newScaleX = 5.0;
                if (newScaleY > 5.0) newScaleY = 5.0;

                elems.MainScaleTransform.ScaleX = newScaleX;
                elems.MainScaleTransform.ScaleY = newScaleY;
            }
        }

        // Tag Dragging Fields
        private bool _isTagDragging = false;
        private SwitchViewModel? _draggedTagSwitch = null;
        private Point _tagDragStartPoint;
        private Point _tagDragOriginalAbsPoint;

        private void Tag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is SwitchViewModel swVm)
            {
                // Selection Logic
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    if (!swVm.IsSelected)
                    {
                        foreach (var element in _viewModel.Elements) element.IsSelected = false;
                        swVm.IsSelected = true;
                    }
                }
                else
                {
                    swVm.IsSelected = !swVm.IsSelected;
                }
                _viewModel.SelectedElement = swVm;
                
                if (!_viewModel.IsEditMode)
                {
                    e.Handled = true;
                    return;
                }

                // Drag Init (Delayed)
                _isTagDragging = false; 
                _draggedTagSwitch = swVm;
                var elems = GetEditorElements(sender);
                if (elems != null)
                {
                    _tagDragStartPoint = e.GetPosition(elems.MainDesigner);
                }
                
                double currentX = swVm.X;
                double currentY = swVm.Y;

                _tagDragOriginalAbsPoint = new Point(currentX, currentY);
                _viewModel.SuppressTopologyUpdates = true;

                el.CaptureMouse();
                e.Handled = true; 
            }
        }

        private void Tag_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTagSwitch != null && sender is FrameworkElement el)
            {
                var elems = GetEditorElements(sender);
                if (elems == null) return;

                if (!_isTagDragging)
                {
                    Point cur = e.GetPosition(elems.MainDesigner);
                    if (Math.Abs(cur.X - _tagDragStartPoint.X) > 5 || Math.Abs(cur.Y - _tagDragStartPoint.Y) > 5)
                    {
                        _isTagDragging = true;
                    }
                }

                if (_isTagDragging)
                {
                    Point currentPos = e.GetPosition(elems.MainDesigner);
                    Point startPoint = _tagDragStartPoint;
                    
                    double deltaX = currentPos.X - startPoint.X;
                    double deltaY = currentPos.Y - startPoint.Y;

                    // Calculate New Absolute Position
                    double newX = _tagDragOriginalAbsPoint.X + deltaX;
                    double newY = _tagDragOriginalAbsPoint.Y + deltaY;

                    // Snap to 1px Grid (Matches tracks)
                    newX = Math.Round(newX);
                    newY = Math.Round(newY);

                    _draggedTagSwitch.X = newX;
                    _draggedTagSwitch.Y = newY;
                }
            }
        }

        private void Tag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTagSwitch != null && sender is FrameworkElement el)
            {
                if (_isTagDragging)
                {
                    // Snap at end too? Already snapped in mouseMove
                }

                _isTagDragging = false;
                _draggedTagSwitch = null;
                el.ReleaseMouseCapture();

                _viewModel.SuppressTopologyUpdates = false;
                _viewModel.UpdateProximitySwitches();
                
                // Add history snapshot for the drag operation
                if (_beforeDragSnapshot != null) _viewModel.AddHistory(_beforeDragSnapshot);
                e.Handled = true;
            }
        }




        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedElement))
            {
                if (_viewModel.SelectedElement != null)
                {
                    ExplorerPane?.SelectElement(_viewModel.SelectedElement);
                }
            }
        }



        private void MainScrollViewer_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Stop BringIntoView bubbles from reaching the Window level ScrollViewer (if any)
            // or causing the MainScrollViewer to jump unexpectedly when child elements get focus.
            e.Handled = true;
        }






        private bool IsOrInsideThumb(DependencyObject? obj)
        {
            while (obj != null)
            {
                if (obj is System.Windows.Controls.Primitives.Thumb) return true;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return false;
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // KeyBindings handle Delete, Ctrl+C, Ctrl+V now.
        }

        private void ViewGraph_Click(object sender, RoutedEventArgs e)
        {
            var graphWin = new GraphWindow(_viewModel);
            graphWin.Owner = this;
            graphWin.Show();
        }
    }
}


