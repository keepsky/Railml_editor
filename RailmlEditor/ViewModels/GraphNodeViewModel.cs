using System.Windows;

namespace RailmlEditor.ViewModels
{
    public class GraphNodeViewModel : ObservableObject
    {
        private string _id;
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private string _label;
        public string Label
        {
            get => _label;
            set => SetProperty(ref _label, value);
        }

        private string _type;
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private double _x;
        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public GraphNodeViewModel(string id, string label, string type, double x = 0, double y = 0)
        {
            Id = id;
            Label = label;
            Type = type;
            X = x;
            Y = y;
        }
    }
}
