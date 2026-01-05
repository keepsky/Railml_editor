using System.Collections.Generic;
using System.Windows;
using RailmlEditor.ViewModels;

namespace RailmlEditor
{
    public partial class JunctionPrincipleSelector : Window
    {
        public TrackViewModel SelectedTrack { get; private set; }

        public JunctionPrincipleSelector(List<TrackViewModel> candidates)
        {
            InitializeComponent();
            
            // Add a helper property for display if not already exists in ViewModel
            // Or just use a wrapper
            var displayItems = new List<TrackDisplayWrapper>();
            foreach (var t in candidates)
            {
                displayItems.Add(new TrackDisplayWrapper { Track = t });
            }
            CandidatesListBox.ItemsSource = displayItems;
            
            if (CandidatesListBox.Items.Count > 0)
            {
                CandidatesListBox.SelectedIndex = 0;
            }
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (CandidatesListBox.SelectedItem is TrackDisplayWrapper wrapper)
            {
                SelectedTrack = wrapper.Track;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("트랙을 선택해주세요.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private class TrackDisplayWrapper
        {
            public TrackViewModel Track { get; set; }
            public string FullDisplayName => $"{Track.Id} ({Track.Name ?? "No Name"})";
        }
    }
}
