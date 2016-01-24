using System;
using System.Collections.Generic;
using AutoMapper;
using Toggl.Phoebe._Data.Models;

namespace Toggl.Phoebe._Data.Json
{
    public static class AutoMapperConfig
    {
        public static void RegisterMappings()
        {
            // TODO: Review how map reverse works with include.
            AutoMapper.Mapper.CreateMap<CommonJson, CommonData>()
            .ForMember (dest => dest.RemoteId, opt => opt.MapFrom (src => src.Id))
            .ForMember (dest => dest.DeletedAt, opt => opt.UseValue (null))
            .Include<ProjectJson, ProjectData>()
            .Include<ClientJson, ClientData>()
            .Include<TagJson, TagData>()
            .Include<TaskJson, TaskData>()
            .Include<WorkspaceJson, WorkspaceData>()
            .Include<WorkspaceUserJson, WorkspaceUserData>()
            .Include<UserJson, UserData>()
            .Include<TimeEntryJson, TimeEntryData>();

            AutoMapper.Mapper.CreateMap<ProjectJson, ProjectData>().ReverseMap();
            AutoMapper.Mapper.CreateMap<ClientJson, ClientData>().ReverseMap();
            AutoMapper.Mapper.CreateMap<TagJson, TagData>().ReverseMap();
            AutoMapper.Mapper.CreateMap<TaskJson, TaskData>().ReverseMap();
            AutoMapper.Mapper.CreateMap<WorkspaceJson, WorkspaceData>().ReverseMap();
            AutoMapper.Mapper.CreateMap<WorkspaceUserJson, WorkspaceUserData>().ReverseMap();

            // User mapping
            AutoMapper.Mapper.CreateMap<UserJson, UserData>();
            AutoMapper.Mapper.CreateMap<UserData, UserJson>()
            .ForMember (dest => dest.CreatedWith, opt => opt.UseValue (Platform.DefaultCreatedWith));

            // TimeEntry mapping
            AutoMapper.Mapper.CreateMap<TimeEntryJson, TimeEntryData>()
            .ForMember (dest => dest.StartTime, opt => opt.ResolveUsing<StartTimeResolver>())
            .ForMember (dest => dest.StopTime, opt => opt.ResolveUsing<StopTimeResolver>())
            .ForMember (dest => dest.State, opt => opt.ResolveUsing<StateResolver>());

            AutoMapper.Mapper.CreateMap<TimeEntryData, TimeEntryJson>()
            .ForMember (dest => dest.CreatedWith, opt => opt.UseValue (Platform.DefaultCreatedWith))
            .ForMember (dest => dest.Duration, opt => opt.ResolveUsing<DurationResolver>());

            // Extra mappings
            AutoMapper.Mapper.CreateMap<ReportJson, ReportData>()
            .ForMember (dest => dest.Activity, opt => opt.ResolveUsing<ReportActivityResolver>())
            .ForMember (dest => dest.Projects, opt => opt.ResolveUsing<ReportProjectsResolver>())
            .ForMember (dest => dest.TotalCost, opt => opt.ResolveUsing<ReportTotalCostResolver>());


            // this is REQUIRED in AutoMapper 4.0 to make inheritance work
            AutoMapper.Mapper.Configuration.Seal();
        }

        #region TimeEntry resolvers
        public class StartTimeResolver : ValueResolver<TimeEntryJson, DateTime>
        {
            protected override DateTime ResolveCore (TimeEntryJson source)
            {
                DateTime startTime;
                if (source.Duration >= 0) {
                    startTime = source.StartTime.ToUtc ();
                } else {
                    var now = Time.UtcNow.Truncate (TimeSpan.TicksPerSecond);
                    var duration = now.ToUnix () + TimeSpan.FromSeconds (source.Duration);
                    startTime = now - duration;
                }
                return startTime;
            }
        }

        public class StopTimeResolver : ValueResolver<TimeEntryJson, DateTime?>
        {
            protected override DateTime? ResolveCore (TimeEntryJson source)
            {
                DateTime? stopTime = null;
                TimeSpan duration;
                if (source.Duration >= 0) {
                    var now = Time.UtcNow.Truncate (TimeSpan.TicksPerSecond);
                    duration = now.ToUnix () + TimeSpan.FromSeconds (source.Duration);
                    stopTime = source.StartTime.ToUtc () + duration;
                }

                return stopTime;
            }
        }

        public class StateResolver : ValueResolver<TimeEntryJson, TimeEntryState>
        {
            protected override TimeEntryState ResolveCore (TimeEntryJson source)
            {
                return source.Duration >= 0 ? TimeEntryState.Finished : TimeEntryState.Running;
            }
        }
        #endregion

        #region Report resolvers
        public class DurationResolver : ValueResolver<TimeEntryData, long>
        {
            protected override long ResolveCore (TimeEntryData source)
            {
                var now = Time.UtcNow;

                // Calculate time entry duration
                TimeSpan duration;
                if (source.StartTime.IsMinValue ()) {
                    duration = TimeSpan.Zero;
                } else {
                    duration = (source.StopTime ?? now) - source.StartTime;
                    if (duration < TimeSpan.Zero) {
                        duration   = TimeSpan.Zero;
                    }
                }

                // Encode the duration
                var encoded = (long)duration.TotalSeconds;
                if (source.State == TimeEntryState.Running) {
                    encoded = (long) (encoded - now.ToUnix ().TotalSeconds);
                }
                return encoded;
            }
        }

        public class ReportActivityResolver : ValueResolver<ReportJson, List<ReportActivity>>
        {
            readonly DateTime UnixStart = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            protected override List<ReportActivity> ResolveCore (ReportJson source)
            {
                var jsonList = source.ActivityContainer;
                var activityList = new List<ReportActivity> ();
                foreach (var item in jsonList.Rows) {
                    activityList.Add (new ReportActivity () {
                        StartTime = UnixStart.AddTicks (ToLong (item[0]) * TimeSpan.TicksPerMillisecond),
                        TotalTime = ToLong (item[1]),
                        BillableTime = ToLong (item[2])
                    });
                }
                return activityList;
            }

            long ToLong (string s)
            {
                long l;

                // round decimal values
                if (!string.IsNullOrEmpty (s) && s.Contains (".")) {
                    double d;
                    double.TryParse (s, out d);
                    l = (long)Math.Round (d);
                } else {
                    long.TryParse (s, out l);
                }
                return l;
            }
        }

        public class ReportProjectsResolver : ValueResolver<ReportJson, List<ReportProject>>
        {
            protected override List<ReportProject> ResolveCore (ReportJson source)
            {
                var jsonList = source.Projects;
                var projectList = new List<ReportProject> ();
                int colorIndex;

                foreach (var item in jsonList) {
                    var p = new ReportProject () {
                        Project = item.Description.Project,
                        TotalTime = item.TotalTime,
                        Color = Int32.TryParse (item.Description.Color, out colorIndex) ? colorIndex : ProjectData.HexColors.Length - 1
                    };
                    p.Items = new List<ReportTimeEntry> ();
                    if (item.Items != null) {
                        foreach (var i in item.Items) {
                            p.Items.Add (new ReportTimeEntry () {
                                Rate = i.Rate,
                                Title = i.Description.Title,
                                Time = i.Time,
                                Sum = i.Sum
                            });
                        }
                    }
                    p.Currencies = new List<ReportCurrency> ();
                    if (item.Currencies != null) {
                        foreach (var i in item.Currencies) {
                            p.Currencies.Add (new ReportCurrency () {
                                Amount = i.Amount,
                                Currency = i.Currency
                            });
                        }
                    }
                    projectList.Add (p);
                }
                return projectList;
            }
        }

        public class ReportTotalCostResolver : ValueResolver<ReportJson, List<string>>
        {
            protected override List<string> ResolveCore (ReportJson source)
            {
                var totalCost = new List<string> ();
                if (source.TotalCurrencies != null) {
                    source.TotalCurrencies.Sort ((x, y) => y.Amount.CompareTo (x.Amount));
                    foreach (var row in source.TotalCurrencies) {
                        totalCost.Add ($"{row.Amount} {row.Currency}");
                    }
                }
                return totalCost;
            }
        }
        #endregion
    }
}
