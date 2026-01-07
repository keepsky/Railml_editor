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

                // Direction "up": arrow points from Begin (x1,y1) to End (x2,y2)
                // Direction "down": arrow points from End (x2,y2) to Begin (x1,y1)
                double anchorX = (vm.Direction == "up") ? x1 : x2;
                double anchorY = (vm.Direction == "up") ? y1 : y2;
                double targetX = (vm.Direction == "up") ? x2 : x1;
                double targetY = (vm.Direction == "up") ? y2 : y1;

                double angle = Math.Atan2(targetY - anchorY, targetX - anchorX) * 180 / Math.PI;

                var transform = new TransformGroup();
                // Rotate around center of the 10x10 polygon
                transform.Children.Add(new RotateTransform(angle, 5, 5)); 
                // Translate so the center of the polygon is at the midpoint of the edge
                transform.Children.Add(new TranslateTransform(xm - 5, ym - 5));
                ArrowHead.RenderTransform = transform;
                
                ArrowHead.Points = new PointCollection(new[] { new Point(0, 0), new Point(10, 5), new Point(0, 10) });
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
