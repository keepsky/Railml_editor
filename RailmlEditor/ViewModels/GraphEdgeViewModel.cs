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

        public GraphEdgeViewModel(GraphNodeViewModel from, GraphNodeViewModel to, string direction = "none")
        {
            FromNode = from;
            ToNode = to;
            Direction = direction;
        }
    }
}
