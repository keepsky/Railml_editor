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
                        Id = $"Tr{_viewModel.Elements.Count + 1}",
                        X = defaultPos.X, 
                        Y = defaultPos.Y,
                        Length = 100 
                    };
                }
                else if (type == "Switch")
                {
                    newElement = new SwitchViewModel
                    {
                        Id = $"Sw{_viewModel.Elements.Count + 1}",
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = $"Sig{_viewModel.Elements.Count + 1}",
                        X = defaultPos.X,
                        Y = defaultPos.Y
                    };
                }
                else if (type == "CurvedTrack")
                {
                    newElement = new CurvedTrackViewModel
                    {
                        Id = $"CurvedTr{_viewModel.Elements.Count + 1}",
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
                        Id = $"Tr{_viewModel.Elements.Count + 1}",
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        Length = 100 // Default Length
                    };
                }
                else if (type == "Switch")
                {
                    newElement = new SwitchViewModel
                    {
                        Id = $"Sw{_viewModel.Elements.Count + 1}",
                        X = dropPosition.X,
                        Y = dropPosition.Y
                    };
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = $"Sig{_viewModel.Elements.Count + 1}",
                        X = dropPosition.X,
                        Y = dropPosition.Y
                    };
                }

                if (newElement != null)
                {
                    _viewModel.Elements.Add(newElement);
                    _viewModel.SelectedElement = newElement; // Auto Select
                }
            }
        }

        private bool _isDragging = false;
        private Point _startPoint;
        private FrameworkElement _draggedControl;

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
                    // If already selected, we don't deselect others to allow dragging the group.
                    
                    _viewModel.SelectedElement = viewModel; // Keep this for property grid focus
                    
                    MainGrid.Focus();
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
                double deltaX = currentPos.X - _startPoint.X;
                double deltaY = currentPos.Y - _startPoint.Y;

                // We want to apply this delta to all selected items, but we need to ensure we don't drift.
                // A better approach is: on MouseDown, store initial positions of ALL selected items.
                // But simplified approach: 
                // We shouldn't change the ViewModel directly with raw delta, we should round the RESULT.
                
                // To support "Moving unit is 10px":
                // 1. Calculate raw new position, then snap.
                // 2. OR Snap the delta.
                
                // User requirement: "Unit is 10px".
                
                // Let's iterate all selected. To avoid drift, we might need a map of OriginalPositions.
                // Since we didn't store a map, let's just do incremental updates? No, that drifts.
                // Let's just use the current positions + delta is risky if we call this 60fps.
                // We need the original positions.
                // Let's store the Start Position of the MOUSE, and calculate Total Delta.
                // Then for each item, TargetPosition = OriginalPosition + TotalDelta.
                // But we don't have OriginalPositions for everyone.
                
                // Quick fix: On MouseDown, capture a dictionary of IDs -> Original Pos?
                // Or just do relative movement with snapping on the DRAG logic?
                
                // Let's try direct modification with snapping. 
                // We need to know the *previous* snapped position to apply delta? 
                
                // Actually, correct way: Store Original positions when Drag starts.
                // Since I can't add fields easily in middle of file, I will just accept slight drift or handle it by resetting the start point?
                // No, resetting start point is bad for snapping.
                
                // Alternative: Only update if delta > 10.
                if (Math.Abs(deltaX) >= 10 || Math.Abs(deltaY) >= 10)
                {
                    double snapX = Math.Round(deltaX / 10.0) * 10.0;
                    double snapY = Math.Round(deltaY / 10.0) * 10.0;

                    if (snapX != 0 || snapY != 0)
                    {
                        foreach (var element in _viewModel.Elements)
                        {
                            if (element.IsSelected)
                            {
                                element.X += snapX;
                                element.Y += snapY;

                                if (element is TrackViewModel trackVm)
                                {
                                    trackVm.X2 += snapX;
                                    trackVm.Y2 += snapY;

                                    if (trackVm is CurvedTrackViewModel curved)
                                    {
                                        curved.MX += snapX;
                                        curved.MY += snapY;
                                    }
                                }
                            }
                        }
                        
                        // Increment start point by the amount we consumed.
                         _startPoint.X += snapX;
                         _startPoint.Y += snapY;
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
    }
}