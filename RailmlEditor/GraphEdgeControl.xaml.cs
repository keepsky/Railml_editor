using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
                
                // Direction "up" means arrow at x2,y2 pointing away from x1,y1
                // Direction "down" means arrow at x1,y1 pointing away from x2,y2
                
                double targetX = (vm.Direction == "up") ? x2 : x1;
                double targetY = (vm.Direction == "up") ? y2 : y1;
                double anchorX = (vm.Direction == "up") ? x1 : x2;
                double anchorY = (vm.Direction == "up") ? y1 : y2;

                double angle = Math.Atan2(targetY - anchorY, targetX - anchorX) * 180 / Math.PI;

                var transform = new TransformGroup();
                transform.Children.Add(new RotateTransform(angle, 5, 2.5)); // Rotate around center of head
                transform.Children.Add(new TranslateTransform(targetX - 5, targetY - 2.5));
                ArrowHead.RenderTransform = transform;
                
                // Adjust points for a better look
                ArrowHead.Points = new PointCollection(new[] { new Point(0, 0), new Point(10, 5), new Point(0, 10) });
            }
        }
    }
}
