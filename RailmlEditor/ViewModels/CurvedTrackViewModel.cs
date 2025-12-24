using System;

namespace RailmlEditor.ViewModels
{
    public class CurvedTrackViewModel : TrackViewModel
    {
        private double _mx;
        public double MX
        {
            get => _mx;
            set { _mx = value; OnPropertyChanged(); OnPropertyChanged(nameof(Length)); }
        }

        private double _my;
        public double MY
        {
            get => _my;
            set { _my = value; OnPropertyChanged(); OnPropertyChanged(nameof(Length)); }
        }

        protected override void FlipHorizontally()
        {
            double oldX = X;
            double oldX2 = X2;
            base.FlipHorizontally();
            MX = oldX + oldX2 - MX;
            OnPropertyChanged(nameof(MX));
        }

        protected override void FlipVertically()
        {
            double oldY = Y;
            double oldY2 = Y2;
            Y = oldY2;
            Y2 = oldY;
            MY = oldY + oldY2 - MY;
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(Y2));
            OnPropertyChanged(nameof(MY));
        }

        public override double Length
        {
            get
            {
                double d1 = Math.Sqrt(Math.Pow(MX - X, 2) + Math.Pow(MY - Y, 2));
                double d2 = Math.Sqrt(Math.Pow(X2 - MX, 2) + Math.Pow(Y2 - MY, 2));
                return d1 + d2;
            }
            set
            {
                // Simple implementation: extend horizontal length from X? 
                // Or maybe extend the SECOND segment?
                // For now, let's keep base behavior (which changes X2) but maybe it's not ideal.
                // Or just ignore setter if we want Strict Geometry.
                // But base setter uses X2 = X + value. This would flatten the track.
                // Let's call base setter for compatibility, users can readjust mid point.
                base.Length = value;
            }
        }
    }
}
