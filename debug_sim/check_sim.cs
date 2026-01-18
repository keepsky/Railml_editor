using System;
using System.IO;
using System.Linq;
using RailmlEditor.Services;
using RailmlEditor.ViewModels;

public class DebugSim
{
    [STAThread] // Required for WPF components
    public static void Main()
    {
        string path = @"C:\WORK\SRC\Railml_editor\sim.railml";
        if (!File.Exists(path))
        {
            Console.WriteLine("sim.railml not found at " + Path.GetFullPath(path));
            return;
        }

        try
        {
            Console.WriteLine("Instantiating MainViewModel...");
            var mainVm = new MainViewModel(); // Might trigger UI logic, but hopefully safe in console
            Console.WriteLine("Instantiating RailmlService...");
            var service = new RailmlService();
            
            Console.WriteLine("Loading sim.railml...");
            var result = service.LoadSnippet(path, mainVm);
            
            Console.WriteLine($"Loaded {result.Count} elements.");
            
            foreach (var el in result.OfType<TrackViewModel>())
            {
                Console.WriteLine($"Track {el.Id}:");
                Console.WriteLine($"  BeginNode AbsPos: {el.BeginNode.AbsPos}");
                Console.WriteLine($"  EndNode AbsPos: {el.EndNode.AbsPos}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Service Error: " + ex.ToString());
        }
    }
}
