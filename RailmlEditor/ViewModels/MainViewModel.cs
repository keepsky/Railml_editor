using System.Collections.ObjectModel;
using System.Windows.Input;
using RailmlEditor.Models;

namespace RailmlEditor.ViewModels
{
    public abstract class BaseElementViewModel : ObservableObject
    {
        private double _x;
        private double _y;
        private string _id;
        private bool _isSelected;

        public double X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        public double Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public abstract string TypeName { get; }
    }

    public class TrackViewModel : BaseElementViewModel
    {
        public override string TypeName => "Track";
        
        private double _length;
        public double Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }
    }

    public class SwitchViewModel : BaseElementViewModel
    {
        public override string TypeName => "Switch";
        // Orientation, BranchAngle etc.
    }

    public class SignalViewModel : BaseElementViewModel
    {
        public override string TypeName => "Signal";
        // Type, Aspect etc.
    }

    public class MainViewModel : ObservableObject
    {
        public ObservableCollection<BaseElementViewModel> Elements { get; } = new ObservableCollection<BaseElementViewModel>();

        private BaseElementViewModel _selectedElement;
        public BaseElementViewModel SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        public ICommand SelectCommand { get; }

        public MainViewModel()
        {
            SelectCommand = new RelayCommand(param => 
            {
                if (param is BaseElementViewModel element)
                {
                    SelectedElement = element;
                }
            });

            // Test Data
            Elements.Add(new TrackViewModel { Id = "Tr01", X = 100, Y = 100, Length = 200 });
        }
    }
}
