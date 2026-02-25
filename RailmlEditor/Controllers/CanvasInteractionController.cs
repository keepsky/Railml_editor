using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Controllers
{
    public class EditorElements
    {
        public Grid MainGrid { get; set; } = null!;
        public ItemsControl MainDesigner { get; set; } = null!;
        public Canvas OverlayCanvas { get; set; } = null!;
        public System.Windows.Shapes.Rectangle SelectionBox { get; set; } = null!;
        public ScrollViewer MainScrollViewer { get; set; } = null!;
        public ScaleTransform MainScaleTransform { get; set; } = null!;
    }

    public class CanvasInteractionController
    {
        private readonly MainViewModel _viewModel;

        // Interaction State
        private bool _isPlacingBorder = false;
        private TrackCircuitBorderViewModel? _ghostBorder;
        
        private bool _isSelecting = false;
        private Point _selectionStartPoint;
        private Point _selectionStartLogicalPoint;
        
        private bool _isPanning = false;
        private Point _panStartPoint;

        private bool _isDragging = false;
        private FrameworkElement? _draggedControl;
        private Point _startPoint;
        private Dictionary<BaseElementViewModel, Point> _originalPositions = new();
        public Dictionary<BaseElementViewModel, Point> OriginalPositions => _originalPositions;

        public CanvasInteractionController(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public T? FindVisualChild<T>(DependencyObject parent, string name) where T : FrameworkElement
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

        public EditorElements? GetEditorElements(object sender)
        {
            if (sender is DependencyObject dObj)
            {
                Grid? mainGrid = sender as Grid;
                if (mainGrid == null || mainGrid.Name != "MainGrid")
                {
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

        public void HandleMainGridMouseMove(object sender, MouseEventArgs e)
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
                var deltaX = (curPos.X - _panStartPoint.X) / elems.MainScaleTransform.ScaleX;
                var deltaY = (curPos.Y - _panStartPoint.Y) / elems.MainScaleTransform.ScaleY;

                if (Math.Abs(deltaX) >= 1.0 || Math.Abs(deltaY) >= 1.0)
                {
                    double snapX = Math.Round(deltaX / 10.0) * 10.0;
                    double snapY = Math.Round(deltaY / 10.0) * 10.0;

                    if (snapX != 0 || snapY != 0)
                    {
                        foreach (var element in _viewModel.Elements)
                        {
                            if (element is SignalViewModel s && !string.IsNullOrEmpty(s.RelatedTrackId)) continue;
                            if (element is TrackCircuitBorderViewModel b && !string.IsNullOrEmpty(b.RelatedTrackId)) continue;
                            element.MoveBy(snapX, snapY);
                        }
                        _panStartPoint.X += snapX;
                        _panStartPoint.Y += snapY;
                    }
                }
            }
        }

        public void HandleMainGridMouseUp(object sender, MouseButtonEventArgs e)
        {
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (_isSelecting)
            {
                _isSelecting = false;
                elems.SelectionBox.Visibility = Visibility.Collapsed;
                
                var curPos = e.GetPosition(elems.MainDesigner);
                var x = Math.Min(curPos.X, _selectionStartLogicalPoint.X);
                var y = Math.Min(curPos.Y, _selectionStartLogicalPoint.Y);
                var w = Math.Abs(curPos.X - _selectionStartLogicalPoint.X);
                var h = Math.Abs(curPos.Y - _selectionStartLogicalPoint.Y);
                var selectRect = new Rect(x, y, w, h);

                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                     _viewModel.ClearAllSelections();
                }

                foreach (var element in _viewModel.Elements)
                {
                    // Fallback width/height since BaseElementViewModel doesn't define it
                    double w_el = 40;
                    double h_el = 20;

                    if (element is TrackViewModel || element is CurvedTrackViewModel)
                    {
                        // For tracks, just use their bounds loosely or let them specify
                        var minX = element.X;
                        var minY = element.Y;
                        w_el = 100; h_el = 100; // rough bounding
                    }
                    else if (element is SwitchViewModel)
                    {
                        w_el = 50; h_el = 50;
                    }

                    var cx = element.X + (w_el / 2);
                    var cy = element.Y + (h_el / 2);
                    if (selectRect.Contains(new Point(cx, cy)))
                    {
                        element.IsSelected = true;
                    }
                }
                
                elems.MainDesigner.ReleaseMouseCapture();
            }
            else if (_isPanning)
            {
                _isPanning = false;
                elems.MainDesigner.ReleaseMouseCapture();
                elems.MainDesigner.Cursor = Cursors.Arrow;
            }
        }
        
        public void HandleMainGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.Captured != null) return;
            var elems = GetEditorElements(sender);
            if (elems == null) return;

            if (e.ChangedButton == MouseButton.Left)
            {
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(elems.OverlayCanvas);
                _selectionStartLogicalPoint = e.GetPosition(elems.MainDesigner);
                
                Canvas.SetLeft(elems.SelectionBox, _selectionStartPoint.X);
                Canvas.SetTop(elems.SelectionBox, _selectionStartPoint.Y);
                elems.SelectionBox.Width = 0;
                elems.SelectionBox.Height = 0;
                elems.SelectionBox.Visibility = Visibility.Visible;

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
                _isPanning = true;
                _panStartPoint = e.GetPosition(elems.MainGrid);
                
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

        public void StartBorderPlacement(EditorElements? elems)
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
            double minDist = 20;
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
                _viewModel.UpdateBorderParent(finalBorder);
            }
        }

        public void HandleItemMouseDown(object sender, MouseButtonEventArgs e, FrameworkElement? element, BaseElementViewModel? viewModel)
        {
            if (IsOrInsideThumb(e.OriginalSource as DependencyObject))
            {
                e.Handled = true; 
                return;
            }

            if (element != null && viewModel != null)
            {
                if (viewModel is SwitchViewModel)
                {
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

                    _viewModel.TakeSnapshot();
                    _isDragging = true;
                    _draggedControl = element;
                    
                    var elems = GetEditorElements(sender);
                    if (elems != null)
                    {
                        _startPoint = e.GetPosition(elems.MainDesigner);
                    }
                    _viewModel.SuppressTopologyUpdates = true;
                    
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

                    _viewModel.SelectedElement = viewModel;
                    
                    elems?.MainDesigner.Focus();
                    element.CaptureMouse();
                    e.Handled = true;
                }
            }
        }

        public void HandleItemMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedControl != null)
            {
                var elems = GetEditorElements(sender);
                if (elems == null) return;
                
                Point currentPos = e.GetPosition(elems.MainDesigner);
                double deltaX = currentPos.X - _startPoint.X;
                double deltaY = currentPos.Y - _startPoint.Y;

                double snapDeltaX = Math.Round(deltaX / 10.0) * 10.0;
                double snapDeltaY = Math.Round(deltaY / 10.0) * 10.0;

                foreach (var kvp in _originalPositions)
                {
                    var element = kvp.Key;
                    var orig = kvp.Value;
                    
                    double proposedX = orig.X + snapDeltaX;
                    double proposedY = orig.Y + snapDeltaY;
                    
                    double shiftX = proposedX - element.X;
                    double shiftY = proposedY - element.Y;
                    
                    element.MoveBy(shiftX, shiftY);
                }
                
                foreach (var kvp in _originalPositions)
                {
                     var element = kvp.Key;
                     if (element is SignalViewModel signalVm)
                     {
                         var oldRelated = signalVm.RelatedTrackId;
                         signalVm.RelatedTrackId = null; 
                         
                         foreach (var other in _viewModel.Elements)
                         {
                             if (other is TrackViewModel t && other != signalVm)
                             {
                                 double distStartSq = Math.Pow(signalVm.X - t.X, 2) + Math.Pow(signalVm.Y - t.Y, 2);
                                 double distEndSq = Math.Pow(signalVm.X - t.X2, 2) + Math.Pow(signalVm.Y - t.Y2, 2);
                                 
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

        private bool IsOrInsideThumb(DependencyObject? obj)
        {
             while (obj != null)
            {
                if (obj is System.Windows.Controls.Primitives.Thumb) return true;
                obj = VisualTreeHelper.GetParent(obj);
            }
            return false;
        }

        public void HandleItemMouseUp(object sender, MouseButtonEventArgs e)
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
            }
        }

        // Thumb Drag State (Absolute)
        private Point _dragStartMousePos;
        private double _dragStartElementX;
        private double _dragStartElementY;
        private double _dragStartElementX2;
        private double _dragStartElementY2;
        private double _dragStartElementMX;
        private double _dragStartElementMY;

        public void HandleThumbDragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _viewModel.TakeSnapshot();
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

        public void HandleStartThumbDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
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

        public void HandleEndThumbDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
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

        public void HandleMidThumbDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
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

        public void HandleThumbDragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             _viewModel.UpdateProximitySwitches();
        }
    }
}
