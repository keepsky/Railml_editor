using System;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 궤도 회로 구역(Area)이 어디서부터 어디까지인지를 나타내는 '경계선(Border)' 요소입니다.
    /// 화면에서 특정 선로(Track) 위에 배치되어, 해당 선로의 어느 지점(Pos)에서 구역이 나뉘는지를 표시합니다.
    /// </summary>
    public class TrackCircuitBorderViewModel : BaseElementViewModel
    {
        public override string TypeName => "Border";

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

        private string? _relatedTrackId;
        /// <summary>이 경계선이 놓여 있는 선로(Track)의 ID입니다.</summary>
        public string? RelatedTrackId
        {
            get => _relatedTrackId;
            set => SetProperty(ref _relatedTrackId, value);
        }

        // pos is calculated relative to track length
        private double _pos;
        /// <summary>연결된 선로의 시작점으로부터 얼마나 멀리 떨어져 있는지를 나타내는 거리값입니다.</summary>
        public double Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
        }

        private double _angle;
        public double Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }
    }
}

