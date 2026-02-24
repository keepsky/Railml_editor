using System;
using System.Windows;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Utils
{
    public static class GeometryUtils
    {
        public static bool IntersectsLine(Rect rect, Point p1, Point p2)
        {
            if (rect.Contains(p1) || rect.Contains(p2)) return true;

            Point topLeft = new Point(rect.Left, rect.Top);
            Point topRight = new Point(rect.Right, rect.Top);
            Point bottomLeft = new Point(rect.Left, rect.Bottom);
            Point bottomRight = new Point(rect.Right, rect.Bottom);

            return LineIntersectsLine(p1, p2, topLeft, topRight) ||
                   LineIntersectsLine(p1, p2, topRight, bottomRight) ||
                   LineIntersectsLine(p1, p2, bottomRight, bottomLeft) ||
                   LineIntersectsLine(p1, p2, bottomLeft, topLeft);
        }

        public static bool LineIntersectsLine(Point a1, Point a2, Point b1, Point b2)
        {
            double d = (a2.X - a1.X) * (b2.Y - b1.Y) - (a2.Y - a1.Y) * (b2.X - b1.X);
            if (d == 0) return false;
            double u = ((b1.X - a1.X) * (b2.Y - b1.Y) - (b1.Y - a1.Y) * (b2.X - b1.X)) / d;
            double v = ((b1.X - a1.X) * (a2.Y - a1.Y) - (b1.Y - a1.Y) * (a2.X - a1.X)) / d;
            return u >= 0 && u <= 1 && v >= 0 && v <= 1;
        }

        public static void GetNearestPointOnTrack(Point p, TrackViewModel track, out Point nearest, out double dist, out double angle)
        {
            double x1 = track.X, y1 = track.Y;
            double x2 = track.X2, y2 = track.Y2;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double lenSq = dx * dx + dy * dy;

            if (lenSq == 0)
            {
                nearest = new Point(x1, y1);
                dist = Math.Sqrt(Math.Pow(p.X - x1, 2) + Math.Pow(p.Y - y1, 2));
                angle = 0;
                return;
            }

            double t = ((p.X - x1) * dx + (p.Y - y1) * dy) / lenSq;
            t = Math.Max(0, Math.Min(1, t));

            nearest = new Point(x1 + t * dx, y1 + t * dy);
            dist = Math.Sqrt(Math.Pow(p.X - nearest.X, 2) + Math.Pow(p.Y - nearest.Y, 2));

            angle = Math.Atan2(dy, dx) * 180 / Math.PI;
        }
    }
}
