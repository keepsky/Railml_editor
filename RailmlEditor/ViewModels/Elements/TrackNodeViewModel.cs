using System.Collections.ObjectModel;
using RailmlEditor.Models;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 선로(Track)의 양 끝단에 달려 있는 '접속점(Node)' 요소입니다.
    /// 다른 선로와 무언가 연결될 때(Connection)이거나, 선로가 끝나는 곳(BufferStop/OpenEnd)인지를 나타냅니다.
    /// </summary>
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
        /// <summary>이 점이 선로의 시작점(Begin)인지 끝점(End)인지를 나타냅니다.</summary>
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
        /// <summary>이 노드가 매달려 있는 주인 선로(Track)의 ID입니다.</summary>
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
        /// <summary>분기기(Switch) 등과 연결될 때, 상대방 선로의 어느 지점을 가리키는지 ID 참조값입니다.</summary>
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

        private double _absPos;
        public double AbsPos
        {
            get => _absPos;
            set => SetProperty(ref _absPos, value);
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

