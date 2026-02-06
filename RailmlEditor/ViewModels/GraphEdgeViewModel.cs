using System.Collections.ObjectModel;

namespace RailmlEditor.ViewModels
{
    public class GraphEdgeViewModel : ObservableObject
    {
        private GraphNodeViewModel _fromNode;
        public GraphNodeViewModel FromNode
        {
            get => _fromNode;
            set => SetProperty(ref _fromNode, value);
        }

        private GraphNodeViewModel _toNode;
        public GraphNodeViewModel ToNode
        {
            get => _toNode;
            set => SetProperty(ref _toNode, value);
        }

        private string _direction; // "up", "down", "none"
        public string Direction
        {
            get => _direction;
            set => SetProperty(ref _direction, value);
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        public ObservableCollection<IntermediateNode> IntermediateNodes { get; } = new ObservableCollection<IntermediateNode>();

        public GraphEdgeViewModel(GraphNodeViewModel from, GraphNodeViewModel to, string direction = "none", string label = "")
        {
            FromNode = from;
            ToNode = to;
            Direction = direction;
            Label = label;
        }
    }

    public class IntermediateNode
    {
        public GraphNodeViewModel Node { get; set; }
        public double Ratio { get; set; } // 0.0 to 1.0
    }
}
