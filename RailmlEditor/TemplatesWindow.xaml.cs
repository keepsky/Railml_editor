using System.Windows;
using System.Windows.Controls;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public partial class TemplatesWindow : Window
    {
        private MainViewModel _viewModel;

        public TemplatesWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            LoadTemplates();
        }

        private void LoadTemplates()
        {
            foreach (TabItem tab in TemplatesTabControl.Items)
            {
                if (tab.Content is TextBox textBox && tab.Tag is string key)
                {
                    // Convert key to lowercase just in case
                    key = key.ToLower();
                    if (_viewModel.CustomTemplates.ContainsKey(key))
                    {
                        textBox.Text = _viewModel.CustomTemplates[key];
                    }
                    else
                    {
                        // Try to load default file content if no custom template yet? 
                        // User request: "User inputs railml... then displays".
                        // So initially empty or default content?
                        // Let's leave empty or try to load default file if exists.
                        string defaultPath = $"{key}.railml";
                        if (System.IO.File.Exists(defaultPath))
                        {
                            textBox.Text = System.IO.File.ReadAllText(defaultPath);
                        }
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (TabItem tab in TemplatesTabControl.Items)
            {
                if (tab.Content is TextBox textBox && tab.Tag is string key)
                {
                    key = key.ToLower();
                    _viewModel.CustomTemplates[key] = textBox.Text;
                }
            }
            _viewModel.SaveCustomTemplates();
            // Optionally close or just indicate saved?
            // "Template selected... then toolbar button pressed uses it".
            // So saving is enough.
            MessageBox.Show("Templates saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
