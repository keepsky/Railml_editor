using RailmlEditor.Models;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services.Mappers
{
    public class SignalMapper : IRailmlElementMapper<SignalViewModel, Signal>
    {
        public void MapToRailml(SignalViewModel source, Signal destination, MappingContext context)
        {
            // Note: As with Switches, Signals are nested inside Tracks (OcsElements.Signals).
            // The instantiation of the Signal model in the RailML tree is done when
            // navigating through the tracks. This mapper is mainly for shared setup 
            // if needed, or visual updates.
            
            // To be consistent with TrackMapper's current logic, 
            // the main visualization generation can be done here if it isn't fully handled by the Track.
        }

        public void MapToViewModel(Signal source, SignalViewModel destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Direction = source.Dir ?? "up";
            destination.Type = source.Type;
            destination.Function = source.Function;
            destination.Name = source.AdditionalName?.Name;

            // X, Y and RelatedTrackId are typically set by the Track deserializer loop
        }
    }
}
