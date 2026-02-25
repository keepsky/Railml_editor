using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;
using System.Collections.Generic;

namespace RailmlEditor.Services.Mappers
{
    /// <summary>
    /// (리팩터링 4단계 핵심) 매퍼 클래스들(TrackMapper, SwitchMapper 등)이 작업을 수행할 때 필요한 '공통 상태 보관소'입니다.
    /// 예전에는 각각의 매개변수로 주렁주렁 넘기거나 RailmlMapper 안에 전역 변수처럼 두었지만, 
    /// 이제는 이 Context 하나만 넘겨주면 어떤 매퍼든 필요한 현재 문서 정보나 상태값을 꺼내볼 수 있습니다.
    /// </summary>
    public class MappingContext
    {
        /// <summary>전체 앱의 메인 뷰모델 (상태 및 ID 생성기 접근 등)</summary>
        public MainViewModel ViewModel { get; }
        /// <summary>현재 활성화된 캔버스의 문서 뷰모델 (요소 목록 등)</summary>
        public DocumentViewModel Document { get; }
        public Railml Railml { get; set; } = null!;
        public Infrastructure Infrastructure => Railml.Infrastructure;
        public LineVis MainLineVis { get; set; } = null!;
        public Visualization MainVis { get; set; } = null!;

        // Shared Dictionaries for Mapping
        /// <summary>ID로 화면 속 뷰모델을 빠르게 찾기 위한 캐시 목록입니다. 매퍼들이 서로 참조할 때 쓰입니다.</summary>
        public Dictionary<string, TrackViewModel> TrackLookup { get; } = new();
        public Dictionary<string, SwitchViewModel> SwitchLookup { get; } = new();
        public Dictionary<string, SignalViewModel> SignalLookup { get; } = new();

        public MappingContext(MainViewModel viewModel, DocumentViewModel document)
        {
            ViewModel = viewModel;
            Document = document;
        }

        public string GetRailmlId(string? prefix)
        {
            return ViewModel.GetNextId(prefix ?? "id");
        }
    }
}
