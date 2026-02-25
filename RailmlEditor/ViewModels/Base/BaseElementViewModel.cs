using System;
using System.ComponentModel;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels
{
    /// <summary>
    /// 이 클래스는 화면(Canvas)에 그려지는 모든 RailML 요소(선로, 스위치, 신호기 등)의 가장 기본이 되는 부모 클래스입니다.
    /// 공통적인 속성인 좌표(X, Y), ID, 이름, 선택 여부 등을 관리합니다.
    /// 
    /// 추상 클래스(abstract)이므로 이 형태 그대로 만들어질 수 없고, 
    /// 반드시 TrackViewModel 같은 자식 클래스가 이를 상속받아 구체화해야 합니다.
    /// </summary>
    public abstract class BaseElementViewModel : ObservableObject
    {
        private double _x;
        private double _y;
        private string? _id;
        private bool _isSelected;

        /// <summary>
        /// 화면 상의 가로(X) 좌표입니다.
        /// 자식 클래스에서 필요에 따라 자유롭게 재정의(override) 할 수 있습니다.
        /// </summary>
        public virtual double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        /// <summary>
        /// 화면 상의 세로(Y) 좌표입니다.
        /// </summary>
        public virtual double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        /// <summary>
        /// (리팩터링 1단계 핵심) 이 요소의 위치를 현재 위치에서 deltaX, deltaY 만큼 이동시키는 명령입니다.
        /// 부모 클래스인 여기서는 단순히 기본 X, Y 좌표만 더해주지만, 
        /// 선로(Track) 같은 자식 클래스에서는 선풍의 끝점(X2, Y2)이나 곡선점도 함께 이동하도록 이 함수를 '덮어쓰기(override)' 하여 사용합니다.
        /// 이렇게 하면 화면 단(View)에서 요소의 내부 구조를 몰라도 단순히 MoveBy()만 호출하여 전체 덩어리를 움직일 수 있습니다.
        /// </summary>
        /// <param name="deltaX">X축으로 이동할 거리</param>
        /// <param name="deltaY">Y축으로 이동할 거리</param>
        public virtual void MoveBy(double deltaX, double deltaY)
        {
            X += deltaX;
            Y += deltaY;
        }

        /// <summary>
        /// RailML 규격에서 사용하는 고유 아이디(ID)입니다. (예: tr1, sw2 등)
        /// </summary>
        public string? Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string? _name;
        /// <summary>
        /// 화면에 표시될 사람이 읽을 수 있는 이름입니다.
        /// </summary>
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 현재 사용자가 이 요소를 마우스로 클릭해서 선택한 상태인지 여부입니다.
        /// 값이 true가 되면 파란색 테두리 등을 그려서 강조 표시에 쓰입니다.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        private bool _showCoordinates;
        /// <summary>
        /// 화면에 현재 요소의 X, Y 좌표값을 글씨로 표시할지 말지를 결정합니다.
        /// </summary>
        public bool ShowCoordinates
        {
            get => _showCoordinates;
            set => SetProperty(ref _showCoordinates, value);
        }

        /// <summary>
        /// 자식 클래스가 반드시 가져야 하는 요소의 종류(예: "Track", "Signal")를 문자열로 반환합니다.
        /// </summary>
        public abstract string TypeName { get; }
    }
}

