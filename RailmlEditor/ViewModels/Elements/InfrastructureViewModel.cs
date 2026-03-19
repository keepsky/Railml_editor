using System.Collections.ObjectModel;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// RailML 문서 전체를 아우르는 가장 큰 개념인 '인프라(Infrastructure)'를 나타냅니다.
    /// 화면 왼쪽의 트리 메뉴(Tree View)에서 루트 노드로 사용되며, 하위에 선로, 스위치, 신호기 등 여러 카테고리로 요소들을 묶어서 보여줍니다.
    /// </summary>
    public class InfrastructureViewModel : BaseElementViewModel
    {
        public override string TypeName => "Infrastructure";
        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
    }

    /// <summary>
    /// 트리 메뉴에서 요소를 종류별(예: "Tracks", "Switches")로 묶어서 보여주기 위한 폴더 역할을 하는 보조 클래스입니다.
    /// </summary>
    public class CategoryViewModel : ObservableObject
    {
        public string? Title { get; set; }
        public ObservableCollection<BaseElementViewModel> Items { get; } = new ObservableCollection<BaseElementViewModel>();
    }
}


