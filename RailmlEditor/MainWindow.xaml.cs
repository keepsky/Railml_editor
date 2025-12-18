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

        private void Toolbox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Button button)
            {
                // Drag Data: Simple String for now, could be an Enum or Complex Object
                string type = button.Content.ToString();
                DragDrop.DoDragDrop(button, type, DragDropEffects.Copy);
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
        private Point _originalElementPos;
        private FrameworkElement _draggedControl;

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is BaseElementViewModel viewModel)
            {
                _isDragging = true;
                _draggedControl = element;
                _startPoint = e.GetPosition(MainDesigner);
                _originalElementPos = new Point(viewModel.X, viewModel.Y);
                
                // Also select the item
                _viewModel.SelectedElement = viewModel;
                
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _draggedControl != null && _draggedControl.DataContext is BaseElementViewModel viewModel)
            {
                Point currentPos = e.GetPosition(MainDesigner);
                double deltaX = currentPos.X - _startPoint.X;
                double deltaY = currentPos.Y - _startPoint.Y;

                viewModel.X = _originalElementPos.X + deltaX;
                viewModel.Y = _originalElementPos.Y + deltaY;
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