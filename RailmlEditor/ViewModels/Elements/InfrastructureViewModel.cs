using System.Collections.ObjectModel;
using RailmlEditor.Utils;

namespace RailmlEditor.ViewModels.Elements
{
    public class InfrastructureViewModel : BaseElementViewModel
    {
        public override string TypeName => "Infrastructure";
        public ObservableCollection<CategoryViewModel> Categories { get; } = new();

        public override double X { get => 0; set { } }
        public override double Y { get => 0; set { } }
    }

    public class CategoryViewModel : ObservableObject
    {
        public string? Title { get; set; }
        public ObservableCollection<BaseElementViewModel> Items { get; } = new ObservableCollection<BaseElementViewModel>();
    }
}


