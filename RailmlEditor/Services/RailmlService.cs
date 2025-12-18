using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Services
{
    public class RailmlService
    {
        public void Save(string path, MainViewModel viewModel)
        {
            var railml = new Railml
            {
                Infrastructure = new Infrastructure
                {
                    Tracks = new Tracks(),
                    Signals = new Signals()
                }
            };

            // Map ViewModels to Data Models
            foreach (var element in viewModel.Elements)
            {
                if (element is TrackViewModel trackVm)
                {
                    var track = new Track
                    {
                        Id = trackVm.Id,
                        Name = "Generated Track",
                        TrackTopology = new TrackTopology
                        {
                            TrackBegin = new TrackNode { X = trackVm.X, Y = trackVm.Y },
                            TrackEnd = new TrackNode { X = trackVm.X + trackVm.Length, Y = trackVm.Y },
                             // Simplified connection for now
                             Connections = new Connections() 
                        }
                    };
                    railml.Infrastructure.Tracks.TrackList.Add(track);
                }
                else if (element is SwitchViewModel switchVm)
                {
                    // Switches are usually child of Connection, but for flattened save we might need complex logic.
                    // For now, let's treat them as simplified if possible or add to a "Dummy" list if RailModel permitted recursion.
                    // Actually, RailML puts switches inside <connections> of a track.
                    // To simplify for this prototype: We will skip saving Switches/Signals correctly nested 
                    // and just save Tracks to prove concept, OR add them to the first track found.
                    // BETTER APPROACH: Add visual coordinates to TrackNode so we can restore them.
                }
                else if (element is SignalViewModel signalVm)
                {
                    var signal = new Signal
                    {
                        Id = signalVm.Id,
                        X = signalVm.X,
                        Y = signalVm.Y
                    };
                    railml.Infrastructure.Signals.SignalList.Add(signal);
                }
            }

            // Serialize
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (TextWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, railml);
            }
        }

        public void Load(string path, MainViewModel viewModel)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Railml));
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                var railml = (Railml)serializer.Deserialize(fs);

                viewModel.Elements.Clear();

                if (railml.Infrastructure?.Tracks?.TrackList != null)
                {
                    foreach (var track in railml.Infrastructure.Tracks.TrackList)
                    {
                        var trackVm = new TrackViewModel
                        {
                            Id = track.Id,
                            X = track.TrackTopology?.TrackBegin?.X ?? 0,
                            Y = track.TrackTopology?.TrackBegin?.Y ?? 0,
                            Length = 100 // Estimate from topology later
                        };
                        
                        // Calculate Length from End - Begin
                        if (track.TrackTopology?.TrackEnd != null)
                        {
                            trackVm.Length = track.TrackTopology.TrackEnd.X - trackVm.X;
                        }

                        viewModel.Elements.Add(trackVm);
                    }
                }

                if (railml.Infrastructure?.Signals?.SignalList != null)
                {
                     foreach (var signal in railml.Infrastructure.Signals.SignalList)
                     {
                         var signalVm = new SignalViewModel
                         {
                             Id = signal.Id,
                             X = signal.X,
                             Y = signal.Y
                         };
                         viewModel.Elements.Add(signalVm);
                     }
                }
            }
        }
    }
}
