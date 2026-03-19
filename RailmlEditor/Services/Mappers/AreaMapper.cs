using RailmlEditor.Models;
using RailmlEditor.ViewModels.Elements;
using System.Linq;

namespace RailmlEditor.Services.Mappers
{
    public class AreaMapper : IRailmlElementMapper<AreaViewModel, Area>
    {
        public void MapToRailml(AreaViewModel source, Area destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Type = source.Type;

            foreach (var b in source.Borders)
            {
                destination.IsLimitedByList.Add(new IsLimitedBy { Ref = b.Id });
            }
        }

        public void MapToViewModel(Area source, AreaViewModel destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Description = source.Description;
            destination.Type = source.Type;

            // Border mapping needs access to the rest of the Elements list to resolve the reference objects.
            foreach (var lim in source.IsLimitedByList)
            {
                var border = context.Document.Elements.OfType<TrackCircuitBorderViewModel>().FirstOrDefault(b => b.Id == lim.Ref);
                if (border != null)
                {
                    destination.Borders.Add(border);
                }
            }
        }
    }
}
