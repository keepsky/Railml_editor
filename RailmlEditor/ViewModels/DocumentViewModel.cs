using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using RailmlEditor.Utils;
using RailmlEditor.ViewModels.Elements;
using System.Windows;
using System.Linq;

namespace RailmlEditor.ViewModels
{
    public class DocumentViewModel : ObservableObject
    {
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public ObservableCollection<BaseElementViewModel> Elements { get; } = new ObservableCollection<BaseElementViewModel>();
        public ObservableCollection<BaseElementViewModel> SelectedElements { get; } = new ObservableCollection<BaseElementViewModel>();

        public ObservableCollection<InfrastructureViewModel> TreeRoots { get; } = new ObservableCollection<InfrastructureViewModel>();

        private InfrastructureViewModel _activeInfrastructure = null!;
        public InfrastructureViewModel ActiveInfrastructure
        {
            get => _activeInfrastructure;
            set => SetProperty(ref _activeInfrastructure, value);
        }

        private BaseElementViewModel? _selectedElement;
        public BaseElementViewModel? SelectedElement
        {
            get => _selectedElement;
            set => SetProperty(ref _selectedElement, value);
        }

        public UndoRedoManager History { get; } = new();

        private string? _filePath;
        public string? FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string InitialTitle { get; set; } = "notitle.railml";

        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set
            {
                if (SetProperty(ref _isDirty, value))
                {
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string Title
        {
            get
            {
                string name = string.IsNullOrEmpty(FilePath) ? InitialTitle : Path.GetFileName(FilePath);
                return name + (IsDirty ? "*" : "");
            }
        }

        public MainViewModel Parent { get; }

        public ICommand CloseCommand { get; }

        public DocumentViewModel(MainViewModel parent)
        {
            Parent = parent;
            
            History.StateChanged += (s, e) => {
                IsDirty = true;
                (Parent.UndoCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (Parent.RedoCommand as RelayCommand)?.RaiseCanExecuteChanged();
            };

            CloseCommand = new RelayCommand(_ => Close());
        }

        public void Close()
        {
            if (IsDirty)
            {
                var result = MessageBox.Show($"Save changes to {Title.TrimEnd('*')} before closing?", "Close Application", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;
                
                if (result == MessageBoxResult.Yes)
                {
                     // Ask MainViewModel to save this document specifically
                     bool success = Parent.SaveDocument(this);
                     if (!success) return; 
                }
            }
            
            Parent.Documents.Remove(this);
        }

    }
}
