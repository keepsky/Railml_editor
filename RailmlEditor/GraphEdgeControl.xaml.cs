using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public partial class GraphEdgeControl : UserControl
    {
        public GraphEdgeControl()
        {
            InitializeComponent();
            this.DataContextChanged += GraphEdgeControl_DataContextChanged;
        }

        private void GraphEdgeControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is GraphEdgeViewModel vm)
            {
                vm.FromNode.PropertyChanged += (s, ev) => UpdateLayout(vm);
                vm.ToNode.PropertyChanged += (s, ev) => UpdateLayout(vm);
                UpdateLayout(vm);
            }
        }

        private void UpdateLayout(GraphEdgeViewModel vm)
        {
            double x1 = vm.FromNode.X;
            double y1 = vm.FromNode.Y;
            double x2 = vm.ToNode.X;
            double y2 = vm.ToNode.Y;

            EdgeLine.X1 = x1;
            EdgeLine.Y1 = y1;
            EdgeLine.X2 = x2;
            EdgeLine.Y2 = y2;

            if (vm.Direction == "none")
            {
                ArrowHead.Visibility = Visibility.Collapsed;
            }
            else
            {
                ArrowHead.Visibility = Visibility.Visible;
                
                // Calculate midpoint
                double xm = (x1 + x2) / 2;
                double ym = (y1 + y2) / 2;

                // User Request:
                // (1) down: trackBegin -> trackEnd (From -> To)
                // (2) up: trackEnd -> trackBegin (To -> From)

                // Assuming FromNode is Begin and ToNode is End (Order in GraphViewModel traversal logic)
                // If From/To order is reversed, we might need checking node type, but simplify assuming standard traversal direction.
                
                double anchorX = (vm.Direction == "down") ? x1 : x2;
                double anchorY = (vm.Direction == "down") ? y1 : y2;
                double targetX = (vm.Direction == "down") ? x2 : x1;
                double targetY = (vm.Direction == "down") ? y2 : y1;

                double angle = Math.Atan2(targetY - anchorY, targetX - anchorX) * 180 / Math.PI;

                var transform = new TransformGroup();
                // Rotate around center of the 10x10 polygon
                transform.Children.Add(new RotateTransform(angle, 5, 5)); 
                
                // Calculate position: Target - Vector * Offset
                // We want the arrow to be near the target node but not overlapping.
                // Node radius is approx 15. Arrow size 10. Let's start with 25 px offset.
                
                double rad = Math.Atan2(targetY - anchorY, targetX - anchorX);
                double offset = 25.0;
                
                double arrowX = targetX - Math.Cos(rad) * offset;
                double arrowY = targetY - Math.Sin(rad) * offset;

                // Translate so the center of the polygon is at the calculated position
                transform.Children.Add(new TranslateTransform(arrowX - 5, arrowY - 5));
                ArrowHead.RenderTransform = transform;
                
                ArrowHead.Points = new PointCollection(new[] { new Point(0, 0), new Point(10, 5), new Point(0, 10) });
            }

            // Update Label Position
            var edgeLabel = this.FindName("LblEdge") as TextBlock;
            if (edgeLabel != null)
            {
                if (!string.IsNullOrEmpty(vm.Label))
                {
                    edgeLabel.Visibility = Visibility.Visible;
                    double xm = (x1 + x2) / 2;
                    double ym = (y1 + y2) / 2;
                    
                    // Center text
                    edgeLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    System.Windows.Controls.Canvas.SetLeft(edgeLabel, xm - edgeLabel.DesiredSize.Width / 2);
                    System.Windows.Controls.Canvas.SetTop(edgeLabel, ym - edgeLabel.DesiredSize.Height / 2);
                }
                else
                {
                    edgeLabel.Visibility = Visibility.Collapsed;
                }
            }

            // Update Intermediate Nodes Positions
            foreach (var node in vm.IntermediateNodes)
            {
                double ix = x1 + (x2 - x1) * node.Ratio;
                double iy = y1 + (y2 - y1) * node.Ratio;
                
                node.Node.X = ix;
                node.Node.Y = iy;
            }
        }
    }
}
