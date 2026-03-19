using System.Collections.ObjectModel;
using System.Collections.Generic;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 기차가 지나가는 특정 구역(Area), 특히 잠금이나 해제를 제어하는 '궤도 회로 구역(Track Circuit Section)'을 나타냅니다.
    /// 화면에 그려질 때 이 구역의 시작과 끝을 나타내는 여러 개의 경계(Borders)를 가질 수 있습니다.
    /// </summary>
    public class AreaViewModel : BaseElementViewModel
    {
        public override string TypeName => "Area";

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _type = "trackSection";
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public List<string> AvailableTypes { get; } = new() { "trackSection", "project" };

        /// <summary>이 구역이 어디서부터 어디까지인지 나타내는 경계(Border) 요소들의 목록입니다.</summary>
        public ObservableCollection<TrackCircuitBorderViewModel> Borders { get; } = new();

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
    }
}





