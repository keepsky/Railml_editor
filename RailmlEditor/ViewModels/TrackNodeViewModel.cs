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

        public TrackNodeViewModel()
        {
            IsIdReadOnly = true; // Default to read-only as per recent user request
        }
        public System.Collections.Generic.IEnumerable<TrackNodeType> AvailableNodeTypes => System.Enum.GetValues(typeof(TrackNodeType)).Cast<TrackNodeType>();
    }
}
