using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailmlEditor.ViewModels;
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
            this.DataContext = _viewModel;
        }

        private Point _toolboxDragStart;
        private Point _startPoint;
        private bool _isDragging;
        private FrameworkElement? _draggedControl;
        private System.Collections.Generic.Dictionary<BaseElementViewModel, Point> _originalPositions = new System.Collections.Generic.Dictionary<BaseElementViewModel, Point>();

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
                    string type = button.Tag.ToString();
                    DragDrop.DoDragDrop(button, type, DragDropEffects.Copy);
                }
            }
        }

        private void Toolbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string type = button.Tag.ToString();
                Point defaultPos = new Point(100, 100);

                BaseElementViewModel newElement = null;

                if (type == "Track")
                {
                    newElement = new TrackViewModel
                    {
                        Id = GetNextId("T"),
                        X = defaultPos.X, 
                        Y = defaultPos.Y,
                        Length = 100 
                    };
                }
                else if (type == "Switch")
                {
                    newElement = new SwitchViewModel
                    {
                        Id = GetNextId("P"),
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = GetNextId("S"),
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "CurvedTrack")
                {
                    double mx = defaultPos.X + 60;
                    double my = defaultPos.Y - 60;
                    newElement = new CurvedTrackViewModel
                    {
                        Id = GetNextId("T"),
                        X = defaultPos.X,
                        Y = defaultPos.Y,
                        // Length removed to avoid overriding X2 logic if Length setter affects it
                        // Set MX, MY, X2, Y2 explicitly
                        MX = mx,
                        MY = my,
                        X2 = mx + 30,
                        Y2 = my
                    };
                }

                if (newElement != null)
                {
                    _viewModel.Elements.Add(newElement);
                    _viewModel.SelectedElement = newElement; 
                    
                    if (newElement is TrackViewModel) _viewModel.UpdateProximitySwitches();
                }
            }
        }

        private void MainDesigner_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string type = (string)e.Data.GetData(DataFormats.StringFormat);
                Point dropPosition = e.GetPosition(MainDesigner);

                BaseElementViewModel newElement = null;

                if (type == "Track")
                {
                    newElement = new TrackViewModel
                    {
                        Id = GetNextId("T"),
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        Length = 100 // Default Length
                    };
                }
                // Switch Removed from Toolbox but ViewModel logic remains if needed
                
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = GetNextId("S"),
                        X = dropPosition.X,
                        Y = dropPosition.Y
                    };
                }

                if (newElement != null)
                {
                    _viewModel.Elements.Add(newElement);
                    _viewModel.SelectedElement = newElement; // Auto Select
                    
                    if (newElement is TrackViewModel) _viewModel.UpdateProximitySwitches();
                }
            }
        }

        private string GetNextId(string prefix)
        {
            return _viewModel.GetNextId(prefix);
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
            
            if (e.ChangedButton == MouseButton.Left)
            {
                // Start Selection
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(OverlayCanvas);
                _selectionStartLogicalPoint = e.GetPosition(MainDesigner);
                
                // Reset styling
                Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
                Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.Visibility = Visibility.Visible;

                // Clear existing selection unless Ctrl or Shift is pressed
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    foreach (var element in _viewModel.Elements)
                    {
                        element.IsSelected = false;
                    }
                    _viewModel.SelectedElement = null;
                }
                
                MainDesigner.Focus();
                MainDesigner.CaptureMouse();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                // Start Panning (Global Move)
                _isPanning = true;
                _panStartPoint = e.GetPosition(MainGrid); // Initial point, acts as "Last Point"
                
                MainDesigner.Focus();
                MainDesigner.CaptureMouse();
                MainDesigner.Cursor = Cursors.SizeAll;
                e.Handled = true;
            }
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                var curPos = e.GetPosition(OverlayCanvas);
                var x = Math.Min(curPos.X, _selectionStartPoint.X);
                var y = Math.Min(curPos.Y, _selectionStartPoint.Y);
                var w = Math.Abs(curPos.X - _selectionStartPoint.X);
                var h = Math.Abs(curPos.Y - _selectionStartPoint.Y);

                Canvas.SetLeft(SelectionBox, x);
                Canvas.SetTop(SelectionBox, y);
                SelectionBox.Width = w;
                SelectionBox.Height = h;
            }
            else if (_isPanning)
            {
                var curPos = e.GetPosition(MainGrid);
                // Divide screen delta by current scale to get logical delta
                var deltaX = (curPos.X - _panStartPoint.X) / MainScaleTransform.ScaleX;
                var deltaY = (curPos.Y - _panStartPoint.Y) / MainScaleTransform.ScaleY;

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
                            else if (element is SwitchViewModel sw)
                            {
                                if (sw.MX.HasValue) sw.MX += snapX;
                                if (sw.MY.HasValue) sw.MY += snapY;
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
            if (_isSelecting)
            {
                _isSelecting = false;
                SelectionBox.Visibility = Visibility.Collapsed;
                MainDesigner.ReleaseMouseCapture();

                // Perform Selection Logic
                Point logicalEnd = e.GetPosition(MainDesigner);
                Rect selectionRect = new Rect(_selectionStartLogicalPoint, logicalEnd);

                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    foreach (var el in _viewModel.Elements) el.IsSelected = false;
                }

                foreach (var element in _viewModel.Elements)
                {
                    bool hit = false;
                    // Check intersection with a small bounding box around primary anchor point (X, Y)
                    Rect anchorBounds = new Rect(element.X - 5, element.Y - 5, 10, 10);
                    if (selectionRect.IntersectsWith(anchorBounds))
                    {
                        hit = true;
                    }
                    else if (element is TrackViewModel track)
                    {
                        // Check end point for tracks
                        Rect endBounds = new Rect(track.X2 - 5, track.Y2 - 5, 10, 10);
                        if (selectionRect.IntersectsWith(endBounds))
                        {
                            hit = true;
                        }
                        
                        // Check intersection with the line segment
                        if (!hit) hit = IntersectsLine(selectionRect, new Point(track.X, track.Y), new Point(track.X2, track.Y2));

                        if (track is CurvedTrackViewModel curved)
                        {
                            // Check midpoint for curved tracks
                            Rect midBounds = new Rect(curved.MX - 5, curved.MY - 5, 10, 10);
                            if (selectionRect.IntersectsWith(midBounds))
                            {
                                hit = true;
                            }
                            
                            // Check both segments of curved track
                            if (!hit)
                            {
                                hit = IntersectsLine(selectionRect, new Point(curved.X, curved.Y), new Point(curved.MX, curved.MY)) ||
                                      IntersectsLine(selectionRect, new Point(curved.MX, curved.MY), new Point(curved.X2, curved.Y2));
                            }
                        }
                    }
                    else if (element is SwitchViewModel sw)
                    {
                        // Also check for the "Point Tag" if it has specific coordinates or default offset
                        double tagX = sw.MX ?? (sw.X - 15.0);
                        double tagY = sw.MY ?? (sw.Y + 7.0);
                        Rect tagBounds = new Rect(tagX, tagY, 35, 12);
                        if (selectionRect.IntersectsWith(tagBounds)) hit = true;
                    }
                    else if (element is SignalViewModel)
                    {
                        // Signal icon is roughly 20x10.
                        Rect signalBounds = new Rect(element.X - 10, element.Y - 5, 20, 10);
                        if (selectionRect.IntersectsWith(signalBounds)) hit = true;
                    }

                    if (hit) element.IsSelected = true;
                }
            }
            else if (_isPanning)
            {
                _isPanning = false;
                MainDesigner.ReleaseMouseCapture();
                MainDesigner.Cursor = Cursors.Arrow;
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
                // Prevent Switch Dragging
                if (viewModel is SwitchViewModel)
                {
                    // Allow selection but prevent drag
                     if (e.ChangedButton == MouseButton.Left)
                    {
                        bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                        if (!viewModel.IsSelected && !isMulti)
                        {
                            foreach (var el in _viewModel.Elements) el.IsSelected = false;
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
                    _isDragging = true;
                    _draggedControl = element;
                    _startPoint = e.GetPosition(MainDesigner);
                    
                    bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                    // If not already selected and multi-select modifier not pressed, select only this.
                    if (!viewModel.IsSelected && !isMulti)
                    {
                        foreach (var el in _viewModel.Elements) el.IsSelected = false;
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
                    
                    MainDesigner.Focus();
                    element.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedControl != null)
            {
                Point currentPos = e.GetPosition(MainDesigner);
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
                                     snapped = true;
                                 }
                                 else if (signalVm.Direction == "down" && distEndSq < 400.0)
                                 {
                                     signalVm.X = t.X2;
                                     signalVm.Y = t.Y2 + 5;
                                     signalVm.RelatedTrackId = t.Id;
                                     snapped = true;
                                 }
                                 
                                 if (snapped) break;
                             }
                         }
                     }
                }
            }
        }



        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _dragStartMousePos = Mouse.GetPosition(MainDesigner);
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
            if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                Point currentMousePos = Mouse.GetPosition(MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                track.X = Math.Round((_dragStartElementX + totalDeltaX) / 10.0) * 10.0;
                track.Y = Math.Round((_dragStartElementY + totalDeltaY) / 10.0) * 10.0;
            }
        }

        private void EndThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                Point currentMousePos = Mouse.GetPosition(MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                track.X2 = Math.Round((_dragStartElementX2 + totalDeltaX) / 10.0) * 10.0;
                track.Y2 = Math.Round((_dragStartElementY2 + totalDeltaY) / 10.0) * 10.0;
            }
        }

        private void MidThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             if (sender is FrameworkElement thumb && thumb.DataContext is CurvedTrackViewModel curved)
             {
                Point currentMousePos = Mouse.GetPosition(MainDesigner);
                double totalDeltaX = (currentMousePos.X - _dragStartMousePos.X);
                double totalDeltaY = (currentMousePos.Y - _dragStartMousePos.Y);

                curved.MX = Math.Round((_dragStartElementMX + totalDeltaX) / 10.0) * 10.0;
                curved.MY = Math.Round((_dragStartElementMY + totalDeltaY) / 10.0) * 10.0;
             }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             _viewModel.UpdateProximitySwitches();
        }

        private void OnPrincipleTrackSelectionRequested(SwitchBranchInfo info)
        {
            // Simple approach: show a ContextMenu at mouse position
            var menu = new ContextMenu();
            var header = new MenuItem { Header = "principle track 선택", IsEnabled = false, FontWeight = FontWeights.Bold };
            menu.Items.Add(header);

            foreach (var cand in info.Candidates)
            {
                var item = new MenuItem { Header = $"{cand.Id}({cand.Name ?? "unnamed"})" };
                item.Click += (s, e) => { 
                    info.Callback(cand); 
                };
                menu.Items.Add(item);
            }

            menu.Closed += (s, e) =>
            {
                // If nothing was selected (Callback not called with non-null), we should rollback
                // Actually, the Callback handles adding the switch. 
                // We need to know if it COMPLETED.
                // Let's check if the switch was added to the collection.
                if (!_viewModel.Elements.Contains(info.Switch))
                {
                    RollbackMove();
                }
            };

            menu.IsOpen = true;
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
                
                // Track moved -> Update topology
                _viewModel.UpdateProximitySwitches();
            }
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "RailML Files (*.xml;*.railml)|*.xml;*.railml|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                var service = new Services.RailmlService();
                try
                {
                    service.Load(dialog.FileName, _viewModel);
                    _viewModel.UpdateProximitySwitches();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}");
                }
            }
        }

        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "RailML Files (*.xml;*.railml)|*.xml;*.railml|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                var service = new Services.RailmlService();
                try
                {
                    service.Save(dialog.FileName, _viewModel);
                    MessageBox.Show("File saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (System.Windows.Input.Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                e.Handled = true;
                double scaleFactor = e.Delta > 0 ? 0.1 : -0.1;
                
                // Get current scale
                double newScaleX = MainScaleTransform.ScaleX + scaleFactor;
                double newScaleY = MainScaleTransform.ScaleY + scaleFactor;

                // Clamp limits (0.2x to 5.0x)
                if (newScaleX < 0.2) newScaleX = 0.2;
                if (newScaleY < 0.2) newScaleY = 0.2;
                if (newScaleX > 5.0) newScaleX = 5.0;
                if (newScaleY > 5.0) newScaleY = 5.0;

                MainScaleTransform.ScaleX = newScaleX;
                MainScaleTransform.ScaleY = newScaleY;
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

                // Drag Init (Delayed)
                _isTagDragging = false; 
                _draggedTagSwitch = swVm;
                _tagDragStartPoint = e.GetPosition(MainDesigner);
                
                double currentMX = swVm.MX ?? (swVm.X - 15.0);
                double currentMY = swVm.MY ?? (swVm.Y + 7.0);

                _tagDragOriginalAbsPoint = new Point(currentMX, currentMY);

                el.CaptureMouse();
                e.Handled = true; 
            }
        }

        private void Tag_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedTagSwitch != null && sender is FrameworkElement el)
            {
                if (!_isTagDragging)
                {
                    Point cur = e.GetPosition(MainDesigner);
                    if (Math.Abs(cur.X - _tagDragStartPoint.X) > 5 || Math.Abs(cur.Y - _tagDragStartPoint.Y) > 5)
                    {
                        _isTagDragging = true;
                    }
                }

                if (_isTagDragging)
                {
                    Point currentPos = e.GetPosition(MainDesigner);
                    Point startPoint = _tagDragStartPoint;
                    
                    double deltaX = currentPos.X - startPoint.X;
                    double deltaY = currentPos.Y - startPoint.Y;

                    // Calculate New Absolute Position
                    double newMX = _tagDragOriginalAbsPoint.X + deltaX;
                    double newMY = _tagDragOriginalAbsPoint.Y + deltaY;

                    // Snap to 5px Grid
                    newMX = Math.Round(newMX / 5.0) * 5.0;
                    newMY = Math.Round(newMY / 5.0) * 5.0;

                    _draggedTagSwitch.MX = newMX;
                    _draggedTagSwitch.MY = newMY;
                }
            }
        }

        private void Tag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedTagSwitch != null && sender is FrameworkElement el)
            {
                if (_isTagDragging)
                {
                    // Reset Condition: "If moved coordinate mx, my is same as pos x,y"
                    if (_draggedTagSwitch.MX.HasValue && _draggedTagSwitch.MY.HasValue)
                    {
                        double dist = Math.Sqrt(Math.Pow(_draggedTagSwitch.MX.Value - _draggedTagSwitch.X, 2) + 
                                                Math.Pow(_draggedTagSwitch.MY.Value - _draggedTagSwitch.Y, 2));
                        
                        if (dist < 5.0)
                        {
                            // Reset to Default
                            _draggedTagSwitch.MX = null;
                            _draggedTagSwitch.MY = null;
                        }
                    }
                }

                _isTagDragging = false;
                _draggedTagSwitch = null;
                el.ReleaseMouseCapture();
                e.Handled = true;
            }
        }


        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is BaseElementViewModel viewModel)
            {
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    foreach (var el in _viewModel.Elements) el.IsSelected = (el == viewModel);
                }
                else
                {
                    viewModel.IsSelected = !viewModel.IsSelected;
                }
                _viewModel.SelectedElement = viewModel;
                
                // Ensure visual focus/highlight
                item.IsSelected = true; 
                item.Focus();
                
                e.Handled = true; 
            }
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

        private bool IntersectsLine(Rect rect, Point p1, Point p2)
        {
            if (rect.Contains(p1) || rect.Contains(p2)) return true;
            Point topLeft = rect.TopLeft;
            Point topRight = rect.TopRight;
            Point bottomLeft = rect.BottomLeft;
            Point bottomRight = rect.BottomRight;
            return LineIntersectsLine(p1, p2, topLeft, topRight) ||
                   LineIntersectsLine(p1, p2, topRight, bottomRight) ||
                   LineIntersectsLine(p1, p2, bottomRight, bottomLeft) ||
                   LineIntersectsLine(p1, p2, bottomLeft, topLeft);
        }

        private bool LineIntersectsLine(Point a1, Point a2, Point b1, Point b2)
        {
            double d = (a2.X - a1.X) * (b2.Y - b1.Y) - (a2.Y - a1.Y) * (b2.X - b1.X);
            if (d == 0) return false;
            double u = ((b1.X - a1.X) * (b2.Y - b1.Y) - (b1.Y - a1.Y) * (b2.X - b1.X)) / d;
            double v = ((b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X)) / d;
            return u >= 0 && u <= 1 && v >= 0 && v <= 1;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // KeyBindings handle Delete, Ctrl+C, Ctrl+V now.
        }
    }
}