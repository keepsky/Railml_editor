using RailmlEditor.ViewModels.Elements;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Services.Mappers
{
    public interface IRailmlElementMapper<TViewModel, TRailmlElement>
        where TViewModel : BaseElementViewModel
    {
        void MapToRailml(TViewModel source, TRailmlElement destination, MappingContext context);
        void MapToViewModel(TRailmlElement source, TViewModel destination, MappingContext context);
    }
}
