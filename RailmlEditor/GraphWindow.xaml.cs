using System.Windows;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public partial class GraphWindow : Window
    {
        public GraphWindow(MainViewModel mainVm)
        {
            InitializeComponent();
            var graphVm = new GraphViewModel();
            this.DataContext = graphVm;
            graphVm.BuildGraph(mainVm.Elements);
        }

        private bool _isDraggingNode;
        private Point _dragStartPoint;
        private GraphNodeViewModel? _draggedNode;

        private void Node_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is GraphNodeViewModel node)
            {
                _isDraggingNode = true;
                _dragStartPoint = e.GetPosition(this);
                _draggedNode = node;
                fe.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Node_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingNode && _draggedNode != null)
            {
                Point currentPoint = e.GetPosition(this);
                double deltaX = currentPoint.X - _dragStartPoint.X;
                double deltaY = currentPoint.Y - _dragStartPoint.Y;

                _draggedNode.X += deltaX;
                _draggedNode.Y += deltaY;

                _dragStartPoint = currentPoint;
            }
        }

        private void Node_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDraggingNode)
            {
                if (sender is FrameworkElement fe)
                {
                    fe.ReleaseMouseCapture();
                }
                _isDraggingNode = false;
                _draggedNode = null;
            }
        }
    }
}
