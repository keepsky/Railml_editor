using System.IO;
using System.Xml.Serialization;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    /// <summary>
    /// RailML 파일을 저장하고 불러올 때 가장 앞장서서 지휘하는 '서비스 계층' 클래스입니다.
    /// 실제로 데이터를 변환하는 작업은 Mapper(매퍼)들에게, XML 파일을 쓰고 읽는 작업은 C#의 기본 기능에게 맡깁니다.
    /// </summary>
    public class RailmlService
    {
        public void Save(string path, MainViewModel viewModel, DocumentViewModel doc)
        {
            var railml = RailmlMapper.ToRailml(viewModel, doc);
            RailmlTopologyBuilder.BuildTopology(railml, doc);

            try
            {
                var serializer = new XmlSerializer(typeof(Railml));
                using var writer = new StreamWriter(path);
                serializer.Serialize(writer, railml);
            }
            catch (System.Exception ex)
            {
                File.WriteAllText("save_error.txt", ex.ToString());
                throw;
            }
        }

        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippet(string path, MainViewModel viewModel, DocumentViewModel doc)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var fs = new FileStream(path, FileMode.Open);
            var railml = (Railml?)serializer.Deserialize(fs);
            return RailmlMapper.ToViewModelsForSnippet(railml, viewModel, doc);
        }

        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippetFromXml(string xmlContent, MainViewModel viewModel, DocumentViewModel doc)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var reader = new StringReader(xmlContent);
            var railml = (Railml?)serializer.Deserialize(reader);
            return RailmlMapper.ToViewModelsForSnippet(railml, viewModel, doc);
        }

        public void Load(string path, MainViewModel viewModel, DocumentViewModel doc)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var fs = new FileStream(path, FileMode.Open);
            var railml = (Railml?)serializer.Deserialize(fs);

            RailmlMapper.LoadIntoViewModel(railml, viewModel, doc);
        }
    }
}
