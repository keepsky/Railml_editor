using RailmlEditor.Models;
using RailmlEditor.ViewModels.Elements;
using System.Linq;

namespace RailmlEditor.Services.Mappers
{
    public class RouteMapper : IRailmlElementMapper<RouteViewModel, Route>
    {
        public void MapToRailml(RouteViewModel source, Route destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Code = source.Code;
            destination.Description = source.Description;
            destination.ApproachPointRef = source.ApproachPointRef;
            destination.EntryRef = source.EntryRef;
            destination.ExitRef = source.ExitRef;
            destination.OverlapEndRef = source.OverlapEndRef;
            destination.ProceedSpeed = source.ProceedSpeed;
            destination.ReleaseTriggerHead = source.ReleaseTriggerHead;
            destination.ReleaseTriggerHeadSpecified = true;
            destination.ReleaseTriggerRef = source.ReleaseTriggerRef;

            foreach (var sV in source.SwitchAndPositions)
            {
                destination.SwitchAndPositionList.Add(new SwitchAndPosition
                {
                    SwitchRef = sV.SwitchRef,
                    SwitchPosition = sV.SwitchPosition
                });
            }

            foreach (var sV in source.OverlapSwitchAndPositions)
            {
                destination.OverlapSwitchAndPositionList.Add(new SwitchAndPosition
                {
                    SwitchRef = sV.SwitchRef,
                    SwitchPosition = sV.SwitchPosition
                });
            }

            if (source.ReleaseSections.Any())
            {
                destination.ReleaseGroup = new ReleaseGroup();
                foreach (var rsV in source.ReleaseSections)
                {
                    destination.ReleaseGroup.TrackSectionRefList.Add(new TrackSectionRef
                    {
                        Ref = rsV.TrackRef,
                        FlankProtection = rsV.FlankProtection
                    });
                }
            }
        }

        public void MapToViewModel(Route source, RouteViewModel destination, MappingContext context)
        {
            destination.Id = source.Id;
            destination.Name = source.Name;
            destination.Code = source.Code;
            destination.Description = source.Description;
            destination.ApproachPointRef = source.ApproachPointRef;
            destination.EntryRef = source.EntryRef;
            destination.ExitRef = source.ExitRef;
            destination.OverlapEndRef = source.OverlapEndRef;
            destination.ProceedSpeed = source.ProceedSpeed ?? "R";
            destination.ReleaseTriggerHead = source.ReleaseTriggerHead;
            destination.ReleaseTriggerRef = source.ReleaseTriggerRef;

            if (source.SwitchAndPositionList != null)
            {
                foreach (var s in source.SwitchAndPositionList)
                {
                    destination.SwitchAndPositions.Add(new SwitchPositionViewModel
                    {
                        SwitchRef = s.SwitchRef,
                        SwitchPosition = s.SwitchPosition,
                        RemoveCommand = new RailmlEditor.ViewModels.RelayCommand(p => { if (p is SwitchPositionViewModel vm) destination.SwitchAndPositions.Remove(vm); })
                    });
                }
            }

            if (source.OverlapSwitchAndPositionList != null)
            {
                foreach (var s in source.OverlapSwitchAndPositionList)
                {
                    destination.OverlapSwitchAndPositions.Add(new SwitchPositionViewModel
                    {
                        SwitchRef = s.SwitchRef,
                        SwitchPosition = s.SwitchPosition,
                        RemoveCommand = new RailmlEditor.ViewModels.RelayCommand(p => { if (p is SwitchPositionViewModel vm) destination.OverlapSwitchAndPositions.Remove(vm); })
                    });
                }
            }

            if (source.ReleaseGroup?.TrackSectionRefList != null)
            {
                foreach (var rs in source.ReleaseGroup.TrackSectionRefList)
                {
                    destination.ReleaseSections.Add(new ReleaseSectionViewModel
                    {
                        TrackRef = rs.Ref,
                        FlankProtection = rs.FlankProtection,
                        RemoveCommand = new RailmlEditor.ViewModels.RelayCommand(p => { if (p is ReleaseSectionViewModel vm) destination.ReleaseSections.Remove(vm); })
                    });
                }
            }
        }
    }
}
