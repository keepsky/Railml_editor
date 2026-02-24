using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Behaviors
{
    public class CanvasDragDropBehavior : Behavior<ItemsControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AllowDrop = true;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Drop -= OnDrop;
            base.OnDetaching();
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (AssociatedObject.DataContext is not MainViewModel viewModel || !viewModel.IsEditMode)
                return;

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                var oldState = viewModel.TakeSnapshot();
                string? type = e.Data.GetData(DataFormats.StringFormat) as string;
                Point dropPosition = e.GetPosition(AssociatedObject);

                BaseElementViewModel? newElement = null;

                if (type == "Track")
                {
                    newElement = new TrackViewModel
                    {
                        Id = viewModel.GetNextId("T"),
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        Length = 100 // Default Length
                    };
                }
                else if (type == "Corner")
                {
                    double mx = dropPosition.X + 20;
                    double my = dropPosition.Y - 40;
                    newElement = new CurvedTrackViewModel
                    {
                        Id = viewModel.GetNextId("T"),
                        Code = "corner",
                        X = dropPosition.X,
                        Y = dropPosition.Y,
                        MX = mx,
                        MY = my,
                        X2 = mx + 10,
                        Y2 = my
                    };
                }
                else if (type == "Single")
                {
                    viewModel.AddDoubleTrack("single.railml", dropPosition);
                }
                else if (type == "SingleR")
                {
                    viewModel.AddDoubleTrack("singleR.railml", dropPosition);
                }
                else if (type == "Signal")
                {
                    newElement = new SignalViewModel
                    {
                        Id = viewModel.GetNextId("S"),
                        X = dropPosition.X,
                        Y = dropPosition.Y
                    };
                }
                else if (type == "Route")
                {
                    newElement = new RouteViewModel
                    {
                        Id = viewModel.GetNextId("R")
                    };
                }
                else if (type == "Double")
                {
                    viewModel.AddDoubleTrack("double.railml", dropPosition);
                }
                else if (type == "DoubleR")
                {
                    viewModel.AddDoubleTrack("doubleR.railml", dropPosition);
                }
                else if (type == "Cross")
                {
                    viewModel.AddDoubleTrack("cross.railml", dropPosition);
                }

                if (newElement != null)
                {
                    viewModel.Elements.Add(newElement);
                    viewModel.SelectedElement = newElement; // Auto Select
                    
                    if (newElement is TrackViewModel) viewModel.UpdateProximitySwitches();
                    viewModel.AddHistory(oldState);
                }
            }
        }
    }
}
