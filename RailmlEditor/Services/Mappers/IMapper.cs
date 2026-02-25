using RailmlEditor.ViewModels.Elements;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Services.Mappers
{
    /// <summary>
    /// RailML 데이터를 변환하는 모든 '매퍼(Mapper)'들이 반드시 지켜야 하는 규칙(인터페이스)입니다.
    /// 화면의 뷰모델(TViewModel)과 실제 파일 구조(TRailmlElement)를 서로 변환해주는 역할을 명시합니다.
    /// </summary>
    /// <typeparam name="TViewModel">화면에 보이는 요소 타입 (예: TrackViewModel)</typeparam>
    /// <typeparam name="TRailmlElement">XML 파일에 저장될 구조체 타입 (예: Track)</typeparam>
    public interface IRailmlElementMapper<TViewModel, TRailmlElement>
        where TViewModel : BaseElementViewModel
    {
        /// <summary>화면의 요소(source)를 보고, 그 정보를 바탕으로 파일 구조체(destination)를 채워 넣습니다.</summary>
        void MapToRailml(TViewModel source, TRailmlElement destination, MappingContext context);
        
        /// <summary>파일 속 구조체(source)를 읽어서, 그 정보를 바탕으로 화면 요소(destination)를 채워 넣습니다.</summary>
        void MapToViewModel(TRailmlElement source, TViewModel destination, MappingContext context);
    }
}
