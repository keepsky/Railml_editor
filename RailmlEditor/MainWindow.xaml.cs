using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using System.Windows.Media;
using RailmlEditor.Controllers;

namespace RailmlEditor
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private RailmlEditor.Controllers.CanvasInteractionController _canvasController;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _canvasController = new RailmlEditor.Controllers.CanvasInteractionController(_viewModel);
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
                    var elems = _canvasController.GetEditorElements(dockManager); // Fallback conceptually to ActiveContent's layout root.
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
                    
                    _canvasController.StartBorderPlacement(null!);
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
            var elems = _canvasController.GetEditorElements(sender);
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


        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _canvasController.HandleMainGridMouseDown(sender, e);
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e)
        {
            _canvasController.HandleMainGridMouseMove(sender, e);
        }

        private void MainGrid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _canvasController.HandleMainGridMouseUp(sender, e);
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
                _canvasController.HandleItemMouseDown(sender, e, element, viewModel);
            }
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            _canvasController.HandleItemMouseMove(sender, e);
        }

        private void Item_MouseUp(object sender, MouseButtonEventArgs e)
        {
             _canvasController.HandleItemMouseUp(sender, e);
        }



        private void Thumb_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
             _canvasController.HandleThumbDragStarted(sender, e);
        }

        private void StartThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             _canvasController.HandleStartThumbDragDelta(sender, e);
        }

        private void EndThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             _canvasController.HandleEndThumbDragDelta(sender, e);
        }

        private void MidThumb_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
             _canvasController.HandleMidThumbDragDelta(sender, e);
        }

        private void Thumb_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
             _canvasController.HandleThumbDragCompleted(sender, e);
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
                RollbackMove(_canvasController.OriginalPositions);
                info.Callback?.Invoke(null);
            }
        }

        private void RollbackMove(System.Collections.Generic.Dictionary<BaseElementViewModel, Point> originalPositions)
        {
            foreach (var kvp in originalPositions)
            {
                var element = kvp.Key;
                var orig = kvp.Value;
                
                double shiftX = orig.X - element.X;
                double shiftY = orig.Y - element.Y;

                element.MoveBy(shiftX, shiftY);
            }
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var elems = _canvasController.GetEditorElements(sender);
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
                var elems = _canvasController.GetEditorElements(sender);
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
                var elems = _canvasController.GetEditorElements(sender);
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


