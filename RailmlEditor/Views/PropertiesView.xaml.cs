using System.Windows;
using System.Windows.Controls;

namespace RailmlEditor.Views
{
    public partial class PropertiesView : UserControl
    {
        public PropertiesView()
        {
            InitializeComponent();
        }

        private void Properties_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Prevent property grid auto-scrolling weirdness if desired,
            // or let it handle it. Original code didn't seem to block it,
            // but usually preventing parents from scrolling when editing a child is good.
            // For now, let's allow it unless it causes issues.
            // e.Handled = true; 
        }
    }
}
