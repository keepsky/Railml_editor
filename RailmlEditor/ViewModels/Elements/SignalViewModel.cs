using System.Collections.Generic;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 기차 길 옆에 서 있는 신호기(Signal)를 나타냅니다.
    /// 어떤 선로 옆(RelatedTrackId)의 어느 위치(Pos)에 서 있는지, 그리고 주로 어떤 기능(Type, Function)을 하는지 기록합니다.
    /// </summary>
    public class SignalViewModel : BaseElementViewModel
    {
        public override string TypeName => "Signal";
        
        // Collections for ComboBox
        public System.Collections.Generic.List<string> AvailableTypes { get; } = new System.Collections.Generic.List<string> 
        { 
            "main", "distant", "repeater", "shunting" 
        };

        public System.Collections.Generic.List<string> AvailableFunctions { get; } = new System.Collections.Generic.List<string> 
        { 
            "exit", "home", "blocking" 
        };

        private string? _type;
        /// <summary>신호기의 종류 (main: 주 신호기, distant: 원방 신호기 등)</summary>
        public string? Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private string? _function;
        /// <summary>신호기의 기능 역할 (exit: 출발, home: 장내 등)</summary>
        public string? Function
        {
            get => _function;
            set => SetProperty(ref _function, value);
        }
        
        private string? _relatedTrackId;
        /// <summary>이 신호기가 세워져 있는 선로(Track)의 ID입니다.</summary>
        public string? RelatedTrackId
        {
            get => _relatedTrackId;
            set => SetProperty(ref _relatedTrackId, value);
        }

        private double _pos;
        /// <summary>연결된 선로의 시작점으로부터 얼마나 떨어진 곳에 있는지를 나타내는 거리값입니다.</summary>
        public double Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
        }

        public System.Collections.Generic.List<string> AvailableDirections { get; } = new System.Collections.Generic.List<string> { "up", "down" };
        
        private string _direction = "up";
        /// <summary>이 신호기가 가리키는 기차의 진행 방향입니다. (up: 상행, down: 하행)</summary>
        public string Direction
        {
            get => _direction;
            set 
            {
                if (SetProperty(ref _direction, value))
                {
                    OnPropertyChanged(nameof(IsFlipped));
                }
            }
        }

        /// <summary>
        /// 방향이 "하행(down)"이면 화면에서 신호기 아이콘을 뒤집어(Flip) 보여주기 위한 속성입니다.
        /// </summary>
        public bool IsFlipped
        {
            get => _direction == "down";
            set => Direction = value ? "down" : "up";
        }
    }
}


