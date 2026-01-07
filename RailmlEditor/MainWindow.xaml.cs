#pragma warning disable
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
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            this.DataContext = _viewModel;
        }

        private Point _toolboxDragStart;
        private Point _startPoint;
        private bool _isDragging;
        private bool _isInternalSelectionChange = false;
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
                    string type = button.Tag.ToString();
                    DragDrop.DoDragDrop(button, type, DragDropEffects.Copy);
                }
            }
        }

        private void Toolbox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var oldState = _viewModel.TakeSnapshot();
                string type = button.Tag.ToString();
                Point defaultPos = new Point(100, 100);

                BaseElementViewModel newElement = null;

                if (type == "Track")
                {
                    newElement = new TrackViewModel
                    {
                        Id = GetNextId("tr"),
                        X = defaultPos.X, 
                        Y = defaultPos.Y,
                        Length = 100 
                    };
                }
                else if (type == "Switch")
                {
                    newElement = new SwitchViewModel
                    {
                        Id = _viewModel.GetNextId("sw"),
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = GetNextId("sig"),
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "Corner")
                {
                    double mx = defaultPos.X + 20;
                    double my = defaultPos.Y - 40;
                    newElement = new CurvedTrackViewModel
                    {
                        Id = GetNextId("tr"),
                        Code = "corner",
                        X = defaultPos.X,
                        Y = defaultPos.Y,
                        MX = mx,
                        MY = my,
                        X2 = mx + 10,
                        Y2 = my
                    };
                }
                else if (type == "Single")
                {
                    _viewModel.AddDoubleTrack("single.railml", defaultPos);
                }
                else if (type == "SingleR")
                {
                    _viewModel.AddDoubleTrack("singleR.railml", defaultPos);
                }
                else if (type == "Route")
                {
                    newElement = new RouteViewModel
                    {
                        Id = GetNextId("R")
                    };
                }
                else if (type == "Double")
                {
                    _viewModel.AddDoubleTrack("double.railml", defaultPos);
                }
                else if (type == "DoubleR")
                {
                    _viewModel.AddDoubleTrack("doubleR.railml", defaultPos);
                }
                else if (type == "Cross")
                {
                    _viewModel.AddDoubleTrack("cross.railml", defaultPos);
                }
                else if (type == "Border")
                {
                    StartBorderPlacement();
                }
                

                if (newElement != null)
                {
                    _viewModel.Elements.Add(newElement);
                    if (newElement is TrackViewModel) _viewModel.UpdateProximitySwitches();
                    _viewModel.AddHistory(oldState);
                }
            }
        }

        private void MainDesigner_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var oldState = _viewModel.TakeSnapshot();
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
                else if (type == "Corner")
                {
                    double mx = dropPosition.X + 20;
                    double my = dropPosition.Y - 40;
                    newElement = new CurvedTrackViewModel
                    {
                        Id = GetNextId("T"),
                        Code = "corner",
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        MX = mx,
                        MY = my,
                        X2 = mx + 10,
                        Y2 = my
                    };
                }
                else if (type == "Single")
                {
                    _viewModel.AddDoubleTrack("single.railml", dropPosition);
                }
                else if (type == "SingleR")
                {
                    _viewModel.AddDoubleTrack("singleR.railml", dropPosition);
                }
                
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = GetNextId("S"),
                        X = dropPosition.X,
                        Y = dropPosition.Y
                    };
                }
                else if (type == "Route")
                {
                    newElement = new RouteViewModel
                    {
                        Id = GetNextId("R")
                    };
                }
                else if (type == "Double")
                {
                    _viewModel.AddDoubleTrack("double.railml", dropPosition);
                }
                else if (type == "DoubleR")
                {
                    _viewModel.AddDoubleTrack("doubleR.railml", dropPosition);
                }
                else if (type == "Cross")
                {
                    _viewModel.AddDoubleTrack("cross.railml", dropPosition);
                }

                if (newElement != null)
                {
                    _viewModel.Elements.Add(newElement);
                    _viewModel.SelectedElement = newElement; // Auto Select
                    
                    if (newElement is TrackViewModel) _viewModel.UpdateProximitySwitches();
                    _viewModel.AddHistory(oldState);
                }
            }
        }

        private string GetNextId(string prefix)
        {
            return _viewModel.GetNextId(prefix);
        }

        private void ElementTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_isInternalSelectionChange) return;

            if (e.NewValue is BaseElementViewModel vm)
            {
                _viewModel.SelectedElement = vm;
                
                // Ensure it's selected on canvas too
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti && !vm.IsSelected)
                {
                    _viewModel.ClearAllSelections();
                    vm.IsSelected = true;
                }
                else if (isMulti)
                {
                    vm.IsSelected = true;
                }
            }
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
                    _viewModel.ClearAllSelections();
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
            
            if (_isPlacingBorder)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    FinalizeBorderPlacement(e.GetPosition(MainDesigner));
                    e.Handled = true;
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    CancelBorderPlacement();
                    e.Handled = true;
                }
            }
        }

        private void StartBorderPlacement()
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
            MainDesigner.Cursor = Cursors.Cross;
        }

        private void CancelBorderPlacement()
        {
            if (_ghostBorder != null) _viewModel.Elements.Remove(_ghostBorder);
            _isPlacingBorder = false;
            _ghostBorder = null;
            MainDesigner.Cursor = Cursors.Arrow;
        }

        private void FinalizeBorderPlacement(Point pos)
        {
            if (_ghostBorder == null) return;

            TrackViewModel? nearestTrack = null;
            double minDist = 20; // Snap distance
            Point snapPoint = pos;
            double snapAngle = 0;

            foreach (var t in _viewModel.Elements.OfType<TrackViewModel>())
            {
                GetNearestPointOnTrack(pos, t, out Point nearest, out double dist, out double angle);
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
                MainDesigner.Cursor = Cursors.Arrow;
                
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
            else if (_isPlacingBorder && _ghostBorder != null)
            {
                var pos = e.GetPosition(MainDesigner);
                _ghostBorder.X = pos.X;
                _ghostBorder.Y = pos.Y;
                _ghostBorder.Angle = 0;
                
                // Snapping preview
                foreach (var t in _viewModel.Elements.OfType<TrackViewModel>())
                {
                    GetNearestPointOnTrack(pos, t, out Point nearest, out double dist, out double angle);
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
                        if (!hit) hit = IntersectsLine(selectionRect, new Point(track.X, track.Y), new Point(track.X2, track.Y2));

                        if (track is CurvedTrackViewModel curved)
                        {
                            // Check midpoint
                            Rect midBounds = new Rect(curved.MX - 5, curved.MY - 5, 10, 10);
                            if (selectionRect.IntersectsWith(midBounds)) hit = true;
                            
                            // Check both segments
                            if (!hit)
                            {
                                hit = IntersectsLine(selectionRect, new Point(curved.X, curved.Y), new Point(curved.MX, curved.MY)) ||
                                      IntersectsLine(selectionRect, new Point(curved.MX, curved.MY), new Point(curved.X2, curved.Y2));
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
                    _beforeDragSnapshot = _viewModel.TakeSnapshot();
                    _isDragging = true;
                    _draggedControl = element;
                    _startPoint = e.GetPosition(MainDesigner);
                    
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
                              GetNearestPointOnTrack(new Point(borderVm.X, borderVm.Y), t, out Point nearest, out double dist, out double angle);
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
             if (_beforeDragSnapshot != null) _viewModel.AddHistory(_beforeDragSnapshot);
        }

        private void OnPrincipleTrackSelectionRequested(SwitchBranchInfo info)
        {
            var selector = new JunctionPrincipleSelector(info.Candidates ?? new System.Collections.Generic.List<TrackViewModel>());
            selector.Owner = this;
            if (selector.ShowDialog() == true)
            {
                info.Callback(selector.SelectedTrack);
            }
            else
            {
                RollbackMove();
                info.Callback(null);
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
                
                _viewModel.UpdateProximitySwitches();
                if (_beforeDragSnapshot != null) _viewModel.AddHistory(_beforeDragSnapshot);
            }
        }

        private string? _currentFilePath = null;

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "RailML Files (*.xml;*.railml)|*.xml;*.railml|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                _currentFilePath = dialog.FileName;
                var service = new Services.RailmlService();
                try
                {
                    service.Load(_currentFilePath, _viewModel);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}");
                }
            }
        }

        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                FileSaveAs_Click(sender, e);
                return;
            }

            var service = new Services.RailmlService();
            try
            {
                service.Save(_currentFilePath, _viewModel);
                MessageBox.Show("File saved successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}");
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
                
                double currentX = swVm.X;
                double currentY = swVm.Y;

                _tagDragOriginalAbsPoint = new Point(currentX, currentY);

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
                    double newX = _tagDragOriginalAbsPoint.X + deltaX;
                    double newY = _tagDragOriginalAbsPoint.Y + deltaY;

                    // Snap to 10px Grid (Matches tracks)
                    newX = Math.Round(newX / 10.0) * 10.0;
                    newY = Math.Round(newY / 10.0) * 10.0;

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
                    if (!viewModel.IsSelected)
                    {
                        _viewModel.ClearAllSelections();
                        viewModel.IsSelected = true;
                    }
                }
                else
                {
                    viewModel.IsSelected = !viewModel.IsSelected;
                }
                _viewModel.SelectedElement = viewModel;
                
                // Ensure visual focus/highlight
                item.IsSelected = true; 
                item.Focus();
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
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.Elements.Count > 0)
            {
                var result = MessageBox.Show("Save changes before creating new project?", "New Project", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Cancel) return;
                if (result == MessageBoxResult.Yes) FileSave_Click(sender, e);
            }
            _viewModel.Elements.Clear();
            _currentFilePath = null; // Need to track current file
        }

        private void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "railway"; 
            dlg.DefaultExt = ".railml"; 
            dlg.Filter = "RailML documents (.railml)|*.railml"; 

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                var service = new Services.RailmlService();
                try
                {
                    service.Save(_currentFilePath, _viewModel);
                    MessageBox.Show("File saved successfully.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }
        }
        private void CreateArea_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateAreaFromSelectedBorders();
        }

        private void ViewGraph_Click(object sender, RoutedEventArgs e)
        {
            var graphWin = new GraphWindow(_viewModel);
            graphWin.Owner = this;
            graphWin.Show();
        }

        private void GetNearestPointOnTrack(Point p, TrackViewModel track, out Point nearest, out double dist, out double angle)
        {
            double x1 = track.X, y1 = track.Y;
            double x2 = track.X2, y2 = track.Y2;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double lenSq = dx * dx + dy * dy;

            if (lenSq == 0)
            {
                nearest = new Point(x1, y1);
                dist = Math.Sqrt(Math.Pow(p.X - x1, 2) + Math.Pow(p.Y - y1, 2));
                angle = 0;
                return;
            }

            double t = ((p.X - x1) * dx + (p.Y - y1) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));

            nearest = new Point(x1 + t * dx, y1 + t * dy);
            dist = Math.Sqrt(Math.Pow(p.X - nearest.X, 2) + Math.Pow(p.Y - nearest.Y, 2));

            angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedElement))
            {
                if (_viewModel.SelectedElement != null)
                {
                    SelectItemInTree(_viewModel.SelectedElement);
                }
            }
        }

        private void SelectItemInTree(object item)
        {
            if (ElementTree == null) return;

            Dispatcher.BeginInvoke(new Action(() => {
                _isInternalSelectionChange = true;
                try
                {
                    var container = GetTreeViewItem(ElementTree, item);
                    if (container != null)
                    {
                        container.IsSelected = true;
                        container.BringIntoView();
                    }
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private TreeViewItem? GetTreeViewItem(ItemsControl parent, object item)
        {
            if (parent == null) return null;

            if (parent.DataContext == item) return parent as TreeViewItem;
            
            if (parent.Items.Contains(item))
            {
                return parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            }

            for (int i = 0; i < parent.Items.Count; i++)
            {
                var childItem = parent.Items[i];
                var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                
                if (container != null)
                {
                    if (container.DataContext == item) return container as TreeViewItem;

                    var found = GetTreeViewItem(container, item);
                    if (found != null) return found;
                }
            }

            return null;
        }
    }
}