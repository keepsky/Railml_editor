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
    /// <summary>
    /// 에디터 화면에 띄워진 하나의 '열린 도화지(파일)'를 나타냅니다.
    /// 이 안에는 도화지 위에 그려진 고유한 요소(Elements) 목록, Undo/Redo 기록(History), 파일 저장 경로(FilePath) 등이 개별적으로 저장됩니다.
    /// 덕분에 크롬 브라우저의 '탭'처럼 여러 개의 문서를 동시에 열어놓고 작업할 수 있습니다.
    /// </summary>
    public class DocumentViewModel : ObservableObject
    {
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>이 문서 도화지 위에 뿌려진 전체 요소(선로, 스위치 등)들의 목록입니다.</summary>
        public ObservableCollection<BaseElementViewModel> Elements { get; } = new ObservableCollection<BaseElementViewModel>();
        
        /// <summary>현재 사용자가 마우스로 드래그하거나 클릭해서 다중 선택한 요소들의 목록입니다.</summary>
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

        /// <summary>Ctrl+Z (되돌리기), Ctrl+Y (다시 실행) 기능을 위해 이 문서에서 일어난 작업 내역을 기억하는 관리자입니다.</summary>
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
