using System.Collections.ObjectModel;
using RailmlEditor.Models;

namespace RailmlEditor.ViewModels
{
    public class TrackNodeViewModel : BaseElementViewModel
    {
        private TrackNodeType _nodeType;
        public TrackNodeType NodeType
        {
            get => _nodeType;
            set
            {
                if (SetProperty(ref _nodeType, value))
                {
                    OnPropertyChanged(nameof(TypeName));
                }
            }
        }

        public override string TypeName => NodeType.ToString();

        private string _role = "Node";
        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        private string? _code;
        public string? Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string? _parentTrackId;
        public string? ParentTrackId
        {
            get => _parentTrackId;
            set => SetProperty(ref _parentTrackId, value);
        }

        private bool _isIdReadOnly;
        public bool IsIdReadOnly
        {
            get => _isIdReadOnly;
            set => SetProperty(ref _isIdReadOnly, value);
        }

        private string? _connectedTrackId;
        public string? ConnectedTrackId
        {
            get => _connectedTrackId;
            set => SetProperty(ref _connectedTrackId, value);
        }

        private string? _connectedNodeId;
        public string? ConnectedNodeId
        {
            get => _connectedNodeId;
            set => SetProperty(ref _connectedNodeId, value);
        }

        private string? _connectionId;
        public string? ConnectionId
        {
            get => _connectionId;
            set => SetProperty(ref _connectionId, value);
        }

        private string? _connectionRef;
        public string? ConnectionRef
        {
            get => _connectionRef;
            set => SetProperty(ref _connectionRef, value);
        }

        private double _pos;
        public double Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
        }

        public TrackNodeViewModel()
        {
            IsIdReadOnly = true; 
        }
        public System.Collections.Generic.IEnumerable<TrackNodeType> AvailableNodeTypes => 
            System.Enum.GetValues(typeof(TrackNodeType))
            .Cast<TrackNodeType>()
            .Where(t => t != TrackNodeType.Connection);
    }
}
