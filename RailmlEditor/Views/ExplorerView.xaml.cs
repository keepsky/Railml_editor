using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Views
{
    public partial class ExplorerView : UserControl
    {
        private MainViewModel? _viewModel => DataContext as MainViewModel;
        private bool _isInternalSelectionChange = false;

        public ExplorerView()
        {
            InitializeComponent();
        }

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is BaseElementViewModel viewModel && _viewModel != null)
            {
                bool isMulti = (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                if (!isMulti)
                {
                    if (!viewModel.IsSelected)
                    {
                        _viewModel.ClearAllSelections();
                        viewModel.IsSelected = true;
                    }
                }
                else
                {
                    viewModel.IsSelected = !viewModel.IsSelected;
                }
                
                // Set Selected for Property Grid
                _viewModel.SelectedElement = viewModel;
                
                // Ensure visual focus/highlight
                item.IsSelected = true; 
                item.Focus();
                
                // IMPORTANT: Do NOT handle the event here if you want DragDrop to work. 
                // However, TreeView selection logic usually wants to be handled. 
                // In MainWindow.xaml.cs, it was handled.
                // If we want to allow Drag from TreeView, we might need to be careful. 
                // For now assuming existing behavior is purely selection.
            }
        }

        private void TreeView_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        public void SelectElement(object item)
        {
             if (ElementTree == null) return;

            Dispatcher.BeginInvoke(new Action(() => {
                _isInternalSelectionChange = true;
                try
                {
                    var container = GetTreeViewItem(ElementTree, item);
                    if (container != null)
                    {
                        container.IsSelected = true;
                        if (!ElementTree.IsKeyboardFocusWithin)
                        {
                            container.BringIntoView();
                        }
                    }
                }
                finally
                {
                    _isInternalSelectionChange = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private TreeViewItem? GetTreeViewItem(ItemsControl parent, object item)
        {
            if (parent == null) return null;

            if (parent.DataContext == item) return parent as TreeViewItem;
            
            if (parent.Items.Contains(item))
            {
                return parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            }

            for (int i = 0; i < parent.Items.Count; i++)
            {
                var childItem = parent.Items[i];
                var container = parent.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                
                if (container != null)
                {
                    if (container.DataContext == item) return container as TreeViewItem;

                    var found = GetTreeViewItem(container, item);
                    if (found != null) return found;
                }
            }

            return null;
        }
    }
}
