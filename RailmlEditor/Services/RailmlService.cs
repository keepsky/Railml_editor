using System.IO;
using System.Xml.Serialization;
using RailmlEditor.Models;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    public class RailmlService
    {
        public void Save(string path, MainViewModel viewModel)
        {
            var railml = RailmlMapper.ToRailml(viewModel);
            RailmlTopologyBuilder.BuildTopology(railml, viewModel);

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

        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippet(string path, MainViewModel viewModel)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var fs = new FileStream(path, FileMode.Open);
            var railml = (Railml?)serializer.Deserialize(fs);
            return RailmlMapper.ToViewModelsForSnippet(railml, viewModel);
        }

        public System.Collections.Generic.List<BaseElementViewModel> LoadSnippetFromXml(string xmlContent, MainViewModel viewModel)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var reader = new StringReader(xmlContent);
            var railml = (Railml?)serializer.Deserialize(reader);
            return RailmlMapper.ToViewModelsForSnippet(railml, viewModel);
        }

        public void Load(string path, MainViewModel viewModel)
        {
            var serializer = new XmlSerializer(typeof(Railml));
            using var fs = new FileStream(path, FileMode.Open);
            var railml = (Railml?)serializer.Deserialize(fs);

            RailmlMapper.LoadIntoViewModel(railml, viewModel);
        }
    }
}
