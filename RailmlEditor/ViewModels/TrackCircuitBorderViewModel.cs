using System;

namespace RailmlEditor.ViewModels
{
    public class TrackCircuitBorderViewModel : BaseElementViewModel
    {
        public override string TypeName => "Border";

        private string? _code;
        public string? Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        private string? _description;
        public string? Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string? _relatedTrackId;
        public string? RelatedTrackId
        {
            get => _relatedTrackId;
            set => SetProperty(ref _relatedTrackId, value);
        }

        // pos is calculated relative to track length
        private double _pos;
        public double Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
        }
    }
}
