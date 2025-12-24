using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
        }

        private Point _toolboxDragStart;
        private Point _startPoint;
        private bool _isDragging;
        private FrameworkElement _draggedControl;
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
                        Id = $"T{_viewModel.Elements.Count + 1:D3}",
                        X = defaultPos.X, 
                        Y = defaultPos.Y,
                        Length = 100 
                    };
                }
                else if (type == "Switch")
                {
                    newElement = new SwitchViewModel
                    {
                        Id = $"P{_viewModel.Elements.Count + 1:D3}", // P001 for Switch
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = $"S{_viewModel.Elements.Count + 1:D3}",
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "CurvedTrack")
                {
                    newElement = new CurvedTrackViewModel
                    {
                        Id = $"PT{_viewModel.Elements.Count + 1:D3}",
                        X = defaultPos.X,
                        Y = defaultPos.Y,
                        Length = 100, 
                        MX = defaultPos.X + 50,
                        MY = defaultPos.Y + 50 
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
                        Id = $"T{_viewModel.Elements.Count + 1:D3}",
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
                        Id = $"S{_viewModel.Elements.Count + 1:D3}",
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


        // Selection & Panning State
        private bool _isSelecting = false;
        private Point _selectionStartPoint;
        private bool _isPanning = false;
        private Point _panStartPoint;

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                // Start Selection
                _isSelecting = true;
                _selectionStartPoint = e.GetPosition(OverlayCanvas);
                
                // Reset styling
                Canvas.SetLeft(SelectionBox, _selectionStartPoint.X);
                Canvas.SetTop(SelectionBox, _selectionStartPoint.Y);
                SelectionBox.Width = 0;
                SelectionBox.Height = 0;
                SelectionBox.Visibility = Visibility.Visible;

                // Clear existing selection unless Ctrl is pressed (standard behavior, though not explicitly requested, good practice)
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
                {
                    foreach (var element in _viewModel.Elements)
                    {
                        element.IsSelected = false;
                    }
                    _viewModel.SelectedElement = null;
                }
                
                MainGrid.Focus();
                MainGrid.CaptureMouse();
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.Middle)
            {
                // Start Panning (Global Move)
                _isPanning = true;
                _panStartPoint = e.GetPosition(MainGrid); // Initial point, acts as "Last Point"
                
                MainGrid.Focus();
                MainGrid.CaptureMouse();
                MainGrid.Cursor = Cursors.SizeAll;
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
                var deltaX = curPos.X - _panStartPoint.X;
                var deltaY = curPos.Y - _panStartPoint.Y;

                if (Math.Abs(deltaX) >= 10 || Math.Abs(deltaY) >= 10)
                {
                    double snapX = Math.Round(deltaX / 10.0) * 10.0;
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
                MainGrid.ReleaseMouseCapture();

                // Perform Selection Logic
                double x = Canvas.GetLeft(SelectionBox);
                double y = Canvas.GetTop(SelectionBox);
                double w = SelectionBox.Width;
                double h = SelectionBox.Height;
                Rect selectionRect = new Rect(x, y, w, h);

                foreach (var element in _viewModel.Elements)
                {
                    // Simple hit testing: center point or bounding box? 
                    // Let's assume point containment (X,Y) for now or simple intersection.
                    // Since elements have different shapes, let's use their X,Y as top-left approx.
                    // Better interaction: Check if element bounds intersect selection rect.
                    // For now, let's check if the element's (X,Y) is inside.
                    if (selectionRect.Contains(new Point(element.X, element.Y)))
                    {
                        element.IsSelected = true;
                    }
                }
            }
            else if (_isPanning)
            {
                _isPanning = false;
                MainGrid.ReleaseMouseCapture();
                MainGrid.Cursor = Cursors.Arrow;
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
            if (sender is FrameworkElement element && element.DataContext is BaseElementViewModel viewModel)
            {
                _isDragging = true;
                _draggedControl = element;
                _startPoint = e.GetPosition(MainDesigner);
                // _originalElementPos won't be enough for multi-select, we need original positions for ALL selected items.
                // But for now, we can calculate deltas from start point.
                
                if (e.ChangedButton == MouseButton.Left)
                {
                    // If not already selected and Ctrl not pressed, select only this.
                    if (!viewModel.IsSelected && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
                    {
                        foreach (var el in _viewModel.Elements) el.IsSelected = false;
                        viewModel.IsSelected = true;
                    }
                    else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                         viewModel.IsSelected = !viewModel.IsSelected;
                    }
                    
                    if (viewModel.IsSelected)
                    {
                        _isDragging = true;
                        _draggedControl = element;
                        _startPoint = e.GetPosition(MainDesigner);
                        
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
                    
                    MainGrid.Focus();
                    // element.CaptureMouse(); // Capturing usually handled by parent or implicit? Explicit for dragging.
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

        private double _thumbAccX;
        private double _thumbAccY;

        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _thumbAccX = 0;
            _thumbAccY = 0;
        }

        private void StartThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                _thumbAccX += e.HorizontalChange;
                _thumbAccY += e.VerticalChange;

                if (Math.Abs(_thumbAccX) >= 10 || Math.Abs(_thumbAccY) >= 10)
                {
                    double snapX = Math.Round(_thumbAccX / 10.0) * 10.0;
                    double snapY = Math.Round(_thumbAccY / 10.0) * 10.0;

                    if (snapX != 0 || snapY != 0)
                    {
                        track.X += snapX;
                        track.Y += snapY;
                        
                        _thumbAccX -= snapX;
                        _thumbAccY -= snapY;
                    }
                }
            }
        }

        private void EndThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             if (sender is FrameworkElement thumb && thumb.DataContext is TrackViewModel track)
            {
                _thumbAccX += e.HorizontalChange;
                _thumbAccY += e.VerticalChange;

                if (Math.Abs(_thumbAccX) >= 10 || Math.Abs(_thumbAccY) >= 10)
                {
                    double snapX = Math.Round(_thumbAccX / 10.0) * 10.0;
                    double snapY = Math.Round(_thumbAccY / 10.0) * 10.0;

                    if (snapX != 0 || snapY != 0)
                    {
                         track.X2 += snapX;
                         track.Y2 += snapY;

                        _thumbAccX -= snapX;
                        _thumbAccY -= snapY;
                    }
                }
            }
        }

        private void MidThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             if (sender is FrameworkElement thumb && thumb.DataContext is CurvedTrackViewModel curved)
             {
                 _thumbAccX += e.HorizontalChange;
                 _thumbAccY += e.VerticalChange;

                 if (Math.Abs(_thumbAccX) >= 10 || Math.Abs(_thumbAccY) >= 10)
                 {
                     double snapX = Math.Round(_thumbAccX / 10.0) * 10.0;
                     double snapY = Math.Round(_thumbAccY / 10.0) * 10.0;

                     if (snapX != 0 || snapY != 0)
                     {
                          curved.MX += snapX;
                          curved.MY += snapY;

                         _thumbAccX -= snapX;
                         _thumbAccY -= snapY;
                     }
                 }
             }
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             _viewModel.UpdateProximitySwitches();
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
    }
}