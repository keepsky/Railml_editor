using System;
using System.Windows;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    public static class ElementFactoryService
    {
        public static BaseElementViewModel? CreateElement(string type, Point position, Func<string, string> idGenerator)
        {
            switch (type)
            {
                case "Track":
                    return new TrackViewModel
                    {
                        Id = idGenerator("tr"),
                        X = position.X,
                        Y = position.Y,
                        Length = 100
                    };
                case "Switch":
                    return new SwitchViewModel
                    {
                        Id = idGenerator("sw"),
                        X = position.X,
                        Y = position.Y
                    };
                case "Signal":
                    return new SignalViewModel
                    {
                        Id = idGenerator("sig"),
                        X = position.X,
                        Y = position.Y
                    };
                case "Corner":
                    double mx = position.X + 20;
                    double my = position.Y - 40;
                    return new CurvedTrackViewModel
                    {
                        Id = idGenerator("tr"),
                        Code = "corner",
                        X = position.X,
                        Y = position.Y,
                        MX = mx,
                        MY = my,
                        X2 = mx + 10,
                        Y2 = my
                    };
                case "Route":
                    return new RouteViewModel
                    {
                        Id = idGenerator("R")
                    };
                default:
                    return null;
            }
        }
    }
}
