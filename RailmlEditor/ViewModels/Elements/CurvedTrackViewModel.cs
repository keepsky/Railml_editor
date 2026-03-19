using System;

namespace RailmlEditor.ViewModels.Elements
{
    /// <summary>
    /// 일반 직선 선로(TrackViewModel)의 기능을 물려받아(상속) 만들어진 '곡선 선로' 클래스입니다.
    /// 직선과 달리 시작점(X, Y)과 끝점(X2, Y2) 외에, 선이 어떻게 휘어질지를 결정하는 '중간 조절점(MX, MY)'을 가지고 있습니다.
    /// 화면에서 이 조절점을 드래그하면 베지어(Bezier) 곡선 형태로 선이 부드럽게 휘어집니다.
    /// </summary>
    public class CurvedTrackViewModel : TrackViewModel
    {
        private double _mx;
        /// <summary>곡선을 만드는 중간 조절점(Control Point)의 X 좌표입니다.</summary>
        public double MX
        {
            get => _mx;
            set 
            { 
                if (SetProperty(ref _mx, value))
                {
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment1Length));
                    OnPropertyChanged(nameof(Segment2Length));
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2));
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1));
                }
            }
        }

        private double _my;
        /// <summary>곡선을 만드는 중간 조절점(Control Point)의 Y 좌표입니다.</summary>
        public double MY
        {
            get => _my;
            set 
            { 
                if (SetProperty(ref _my, value))
                {
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment1Length));
                    OnPropertyChanged(nameof(Segment2Length));
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2));
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1));
                }
            }
        }

        public override double X
        {
            get => base.X;
            set 
            { 
                if (base.X != value)
                {
                    base.X = value; 
                    OnPropertyChanged(nameof(X));
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment1Length)); 
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2)); 
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1)); 
                }
            }
        }
        public override double Y
        {
            get => base.Y;
            set 
            { 
                if (base.Y != value)
                {
                    base.Y = value; 
                    OnPropertyChanged(nameof(Y));
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment1Length)); 
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2)); 
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1)); 
                }
            }
        }
        public override double X2
        {
            get => base.X2;
            set 
            { 
                if (base.X2 != value)
                {
                    base.X2 = value; 
                    OnPropertyChanged(nameof(X2));
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment2Length)); 
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2)); 
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1)); 
                }
            }
        }
        public override double Y2
        {
            get => base.Y2;
            set 
            { 
                if (base.Y2 != value)
                {
                    base.Y2 = value; 
                    OnPropertyChanged(nameof(Y2));
                    OnPropertyChanged(nameof(Length));
                    OnPropertyChanged(nameof(Segment2Length)); 
                    OnPropertyChanged(nameof(Segment1LengthGreaterThan2)); 
                    OnPropertyChanged(nameof(Segment2LengthGreaterThan1)); 
                }
            }
        }

        public double Segment1Length => Math.Sqrt(Math.Pow(MX - X, 2) + Math.Pow(MY - Y, 2));
        public double Segment2Length => Math.Sqrt(Math.Pow(X2 - MX, 2) + Math.Pow(Y2 - MY, 2));

        public bool Segment1LengthGreaterThan2 => Segment1Length >= Segment2Length;
        public bool Segment2LengthGreaterThan1 => Segment2Length > Segment1Length;

        public override void MoveBy(double deltaX, double deltaY)
        {
            base.MoveBy(deltaX, deltaY);
            MX += deltaX;
            MY += deltaY;
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

