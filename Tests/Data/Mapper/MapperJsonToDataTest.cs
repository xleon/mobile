using System;
using System.Collections.Generic;
using NUnit.Framework;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Generic;
using Toggl.Phoebe._Data.Json;
using Toggl.Phoebe._Data.Models;
using XPlatUtils;

namespace Toggl.Phoebe.Tests.Data.Mapper
{
    public class MapperJsonToDataTest : Test
    {
        JsonMapper mapper;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            ServiceContainer.Register<IPlatformUtils> (new PlatformUtils());
            mapper = new JsonMapper ();
        }

        [Test]
        public void TestCommonDataMap()
        {
            CommonJson commonJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var commonData = mapper.Map<CommonData> (commonJson);

            Assert.That (commonData.RemoteId, Is.EqualTo (commonJson.RemoteId));
            Assert.That (commonData.ModifiedAt, Is.EqualTo (commonJson.ModifiedAt.ToUtc()));
            Assert.That (commonData.DeletedAt, Is.Null);
            Assert.That (commonData.IsDirty, Is.False);
            Assert.That (commonData.RemoteRejected, Is.False);
            Assert.That (commonData.Id, Is.EqualTo (Guid.Empty));

            commonJson.DeletedAt = DateTime.Now;
            commonData = mapper.Map<CommonData> (commonJson);
            Assert.That (commonData.DeletedAt, Is.EqualTo (commonJson.DeletedAt.ToUtc ()));
        }

        [Test]
        public void TestMappingInheritance()
        {
            // Check that rules defined for CommonJson -> CommonData
            // works for a descendent object like ClientData.
            var clientJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var clientData = mapper.Map<ClientData> (clientJson);

            Assert.That (clientData.RemoteId, Is.EqualTo (clientJson.RemoteId));
            Assert.That (clientData.ModifiedAt, Is.EqualTo (clientJson.ModifiedAt.ToUtc()));
            Assert.That (clientData.DeletedAt, Is.Null);
            Assert.That (clientData.IsDirty, Is.False);
            Assert.That (clientData.RemoteRejected, Is.False);
            Assert.That (clientData.Id, Is.EqualTo (Guid.Empty));

            clientJson.DeletedAt = DateTime.Now;
            clientData = mapper.Map<ClientData> (clientJson);
            Assert.That (clientData.DeletedAt, Is.EqualTo (clientJson.DeletedAt.ToUtc ()));
        }

        [Test]
        public void TestClientDataMap()
        {
            var clientJson = new ClientJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = null,
                Name = "Client",
                WorkspaceRemoteId = 321
            };

            var clientData = mapper.Map<ClientData> (clientJson);

            Assert.That (clientData.Name, Is.EqualTo (clientJson.Name));
            Assert.That (clientData.WorkspaceRemoteId, Is.EqualTo (clientJson.WorkspaceRemoteId));
            Assert.That (clientData.WorkspaceId, Is.EqualTo (Guid.Empty));
        }

        [Test]
        public void TestProjectDataMap()
        {
            var projectJson = new ProjectJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Project",
                WorkspaceRemoteId = 321,
                ClientRemoteId = 333,
                IsActive = true,
                IsPrivate = true,
                Color = "1",
                IsBillable = true,
                IsTemplate = true,
                UseTasksEstimate = true
            };

            var projectData = mapper.Map<ProjectData> (projectJson);

            Assert.That (projectData.Name, Is.EqualTo (projectJson.Name));
            Assert.That (projectData.IsActive, Is.EqualTo (projectJson.IsActive));
            Assert.That (projectData.IsPrivate, Is.EqualTo (projectJson.IsPrivate));
            Assert.That (projectData.IsBillable, Is.EqualTo (projectJson.IsBillable));
            Assert.That (projectData.IsTemplate, Is.EqualTo (projectJson.IsTemplate));
            Assert.That (projectData.UseTasksEstimate, Is.EqualTo (projectJson.UseTasksEstimate));
            Assert.That (projectData.Color, Is.EqualTo (int.Parse (projectJson.Color)));
            Assert.That (projectData.ClientRemoteId, Is.EqualTo (projectJson.ClientRemoteId));
            Assert.That (projectData.ClientId, Is.EqualTo (Guid.Empty));
            Assert.That (projectData.WorkspaceRemoteId, Is.EqualTo (projectJson.WorkspaceRemoteId));
            Assert.That (projectData.WorkspaceId, Is.EqualTo (Guid.Empty));
        }

        [Test]
        public void TestTagDataMap()
        {
            var tagJson = new TagJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Tag",
                WorkspaceRemoteId = 321,
            };

            var tagData = mapper.Map<TagData> (tagJson);

            Assert.That (tagData.Name, Is.EqualTo (tagJson.Name));
            Assert.That (tagData.WorkspaceRemoteId, Is.EqualTo (tagJson.WorkspaceRemoteId));
            Assert.That (tagData.WorkspaceId, Is.EqualTo (Guid.Empty));

        }

        [Test]
        public void TestTaskDataMap()
        {
            var taskJson = new TaskJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "Task",
                WorkspaceRemoteId = 321,
                ProjectRemoteId = 111,
                Estimate = 12,
                IsActive = true
            };

            var taskData = mapper.Map<TaskData> (taskJson);

            Assert.That (taskData.Name, Is.EqualTo (taskJson.Name));
            Assert.That (taskData.WorkspaceRemoteId, Is.EqualTo (taskJson.WorkspaceRemoteId));
            Assert.That (taskData.WorkspaceId, Is.EqualTo (Guid.Empty));
            Assert.That (taskData.ProjectId, Is.EqualTo (Guid.Empty));
            Assert.That (taskData.ProjectRemoteId, Is.EqualTo (taskJson.ProjectRemoteId));
            Assert.That (taskData.IsActive, Is.EqualTo (taskJson.IsActive));
            Assert.That (taskData.Estimate, Is.EqualTo (taskJson.Estimate));
        }

        [Test]
        public void TestWorkspaceUserDataMap()
        {
            var workspaceUserJson = new WorkspaceUserJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "WsU",  // Not used on WorkspaceUserData
                WorkspaceRemoteId = 321,
                IsActive = true,
                Email = "support@toggl.com",
                IsAdmin = true,
                UserRemoteId = 123
            };

            var workspaceUserData = mapper.Map<WorkspaceUserData> (workspaceUserJson);

            Assert.That (workspaceUserData.IsActive, Is.EqualTo (workspaceUserJson.IsActive));
            Assert.That (workspaceUserData.IsAdmin, Is.EqualTo (workspaceUserJson.IsAdmin));
            Assert.That (workspaceUserData.UserRemoteId, Is.EqualTo (workspaceUserJson.UserRemoteId));
            Assert.That (workspaceUserData.WorkspaceRemoteId, Is.EqualTo (workspaceUserJson.WorkspaceRemoteId));
            Assert.That (workspaceUserData.WorkspaceId, Is.EqualTo (Guid.Empty));
            Assert.That (workspaceUserData.Email, Is.EqualTo (workspaceUserJson.Email));
            Assert.That (workspaceUserData.UserRemoteId, Is.EqualTo (workspaceUserJson.UserRemoteId));
            Assert.That (workspaceUserData.UserId, Is.EqualTo (Guid.Empty));
        }

        [Test]
        public void TestWorkspaceDataMap()
        {
            var workspaceJson = new WorkspaceJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "WsU",
                IsAdmin = true,
                DefaultCurrency = "euro",
                DefaultRate = 1,
                IsPremium = true,
                LogoUrl = "http://logout.com",
                OnlyAdminsMayCreateProjects = true,
                OnlyAdminsSeeBillableRates = true,
                RoundingMode = RoundingMode.Up,
                RoundingPercision = 1
            };

            var wData = mapper.Map<WorkspaceData> (workspaceJson);

            Assert.That (wData.IsPremium, Is.EqualTo (workspaceJson.IsPremium));
            Assert.That (wData.DefaultRate, Is.EqualTo (workspaceJson.DefaultRate));
            Assert.That (wData.DefaultCurrency, Is.EqualTo (workspaceJson.DefaultCurrency));
            Assert.That (wData.IsAdmin, Is.EqualTo (workspaceJson.IsAdmin));
            Assert.That (wData.Name, Is.EqualTo (workspaceJson.Name));
            Assert.That (wData.LogoUrl, Is.EqualTo (workspaceJson.LogoUrl));
            Assert.That (wData.OnlyAdminsMayCreateProjects, Is.EqualTo (workspaceJson.OnlyAdminsMayCreateProjects));
            Assert.That (wData.OnlyAdminsSeeBillableRates, Is.EqualTo (workspaceJson.OnlyAdminsSeeBillableRates));
            Assert.That (wData.RoundingMode, Is.EqualTo (workspaceJson.RoundingMode));
            Assert.That (wData.RoundingPercision, Is.EqualTo (workspaceJson.RoundingPercision));
        }

        [Test]
        public void TestUserDataMap()
        {
            var obmJson = new OBMJson {
                Included = true,
                Number = 10
            };

            var userJson = new UserJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Name = "User",
                ApiToken = "123",
                Email = "support@toggl.com",
                DateFormat = "MM-DD-YY",
                DurationFormat = _Data.DurationFormat.Classic,
                GoogleAccessToken = "GoogleToken",
                DefaultWorkspaceRemoteId = 111,
                ImageUrl = "image.jpg",
                Locale = "locale",
                OBM = obmJson,
                Password = "pass", // Not used on UserData
                SendProductEmails = true,
                SendTimerNotifications = true,
                SendWeeklyReport = true,
                StartOfWeek = DayOfWeek.Monday,
                StoreStartAndStopTime = true,
                TimeFormat = "MM:SS",
                Timezone = "TimeZone"
            };

            var userData = mapper.Map<UserData> (userJson);

            Assert.That (userData.ApiToken, Is.EqualTo (userJson.ApiToken));
            Assert.That (userData.DateFormat, Is.EqualTo (userJson.DateFormat));
            Assert.That (userData.DefaultWorkspaceId, Is.EqualTo (Guid.Empty));
            Assert.That (userData.DefaultWorkspaceRemoteId, Is.EqualTo (userJson.DefaultWorkspaceRemoteId));
            Assert.That (userData.DurationFormat, Is.EqualTo (userJson.DurationFormat));
            Assert.That (userData.Email, Is.EqualTo (userJson.Email));
            Assert.That (userData.ExperimentIncluded, Is.EqualTo (userJson.OBM.Included));
            Assert.That (userData.ExperimentNumber, Is.EqualTo (userJson.OBM.Number));
            Assert.That (userData.GoogleAccessToken, Is.EqualTo (userJson.GoogleAccessToken));
            Assert.That (userData.ImageUrl, Is.EqualTo (userJson.ImageUrl));
            Assert.That (userData.Locale, Is.EqualTo (userJson.Locale));
            Assert.That (userData.Name, Is.EqualTo (userJson.Name));
            Assert.That (userData.SendProductEmails, Is.EqualTo (userJson.SendProductEmails));
            Assert.That (userData.SendTimerNotifications, Is.EqualTo (userJson.SendTimerNotifications));
            Assert.That (userData.StartOfWeek, Is.EqualTo (userJson.StartOfWeek));
            Assert.That (userData.SendWeeklyReport, Is.EqualTo (userJson.SendWeeklyReport));
            Assert.That (userData.TrackingMode, Is.EqualTo (TrackingMode.StartNew));
            Assert.That (userData.TimeFormat, Is.EqualTo (userJson.TimeFormat));
            Assert.That (userData.Timezone, Is.EqualTo (userJson.Timezone));

            // Test Continue mode.
            userJson.StoreStartAndStopTime = false;
            userData = mapper.Map<UserData> (userJson);
            Assert.That (userData.TrackingMode, Is.EqualTo (TrackingMode.Continue));

        }

        [Test]
        public void TestTimeEntryDataMap()
        {
            var tags = new System.Collections.Generic.List<string> {"tag1", "tag2", "tag3"};
            var duration = TimeSpan.FromMinutes (3);
            var startTime = new DateTime (DateTime.Now.Ticks);
            var stopTime = startTime + duration;
            var runningDuration = -Convert.ToInt64 ((startTime.ToUtc().Subtract (new DateTime (1970,1,1,0,0,0))).TotalSeconds);

            var teJson = new TimeEntryJson {
                RemoteId = 123,
                ModifiedAt = DateTime.Now,
                DeletedAt = DateTime.Now,
                Description = "description",
                Duration = Convert.ToInt64 (duration.TotalSeconds), // Duration comes in seconds!
                IsBillable = true,
                ProjectRemoteId = 123,
                DurationOnly = true,
                StartTime = startTime,
                StopTime = stopTime,
                Tags = tags,
                TaskRemoteId = null,
                UserRemoteId = 333,
                WorkspaceRemoteId = 222
            };

            var teData = mapper.Map<TimeEntryData> (teJson);

            Assert.That (teData.Description, Is.EqualTo (teJson.Description));
            Assert.That (teData.DurationOnly, Is.EqualTo (teJson.DurationOnly));
            Assert.That (teData.IsBillable, Is.EqualTo (teJson.IsBillable));
            Assert.That (teData.ProjectId, Is.EqualTo (Guid.Empty));
            Assert.That (teData.ProjectRemoteId, Is.EqualTo (teJson.ProjectRemoteId));
            Assert.That (teData.StartTime, Is.EqualTo (teJson.StartTime.ToUtc ()));
            Assert.That (teData.StopTime, Is.EqualTo (teJson.StopTime.ToUtc ()));
            Assert.That (teData.State, Is.EqualTo (TimeEntryState.Finished));
            Assert.That (teData.Tags, Is.EqualTo (tags));
            Assert.That (teData.TaskId, Is.EqualTo (Guid.Empty));
            Assert.That (teData.TaskRemoteId, Is.EqualTo (teJson.TaskRemoteId));
            Assert.That (teData.UserId, Is.EqualTo (Guid.Empty));
            Assert.That (teData.UserRemoteId, Is.EqualTo (teJson.UserRemoteId));
            Assert.That (teData.WorkspaceId, Is.EqualTo (Guid.Empty));
            Assert.That (teData.WorkspaceRemoteId, Is.EqualTo (teJson.WorkspaceRemoteId));

            teJson.Duration = runningDuration;
            teData = mapper.Map<TimeEntryData> (teJson);

            //TODO: check startTime.
            Assert.That (teData.StopTime, Is.Null);
            Assert.That (teData.State, Is.EqualTo (TimeEntryState.Running));
        }

        [Test]
        public void TestReportJsonDataMap ()
        {
            var row1 = new List<string> {"111", "111", "111"};
            var row2 = new List<string> {"222", "222", "222"};
            var row3 = new List<string> {"333", "333", "333"};

            var reportProject1 = new ReportProjectJson {
                Description = new ReportProjectDescJson {
                    Client = "client1",
                    Color = "color1",
                    Project = "reportProject1"
                },
                Id = "reportProject1",
                Currencies = new List<ReportCurrencyJson> {
                    new ReportCurrencyJson { Amount = 10, Currency = "euro" },
                    new ReportCurrencyJson { Amount = 100, Currency = "dollar" },
                },
                Items = new List<ReportTimeEntryJson> {
                    new ReportTimeEntryJson {
                        Currency = "curr",
                        Description = new ReportTimeEntryDescJson { Title = "Title" },
                        Ids="1,1,1",
                        Rate = 1.0f,
                        Sum = 1.0f,
                        Time = 1111
                    }
                }
            };

            var reportProject2 = new ReportProjectJson {
                Description = new ReportProjectDescJson {
                    Client = "client2",
                    Color = "color2",
                    Project = "reportProject2"
                },
                Id = "reportProject2",
                Currencies = new List<ReportCurrencyJson> {
                    new ReportCurrencyJson { Amount = 20, Currency = "euro" },
                    new ReportCurrencyJson { Amount = 200, Currency = "dollar" },
                },
                Items = new List<ReportTimeEntryJson> {
                    new ReportTimeEntryJson {
                        Currency = "curr",
                        Description = new ReportTimeEntryDescJson { Title = "Title" },
                        Ids="2,2,2",
                        Rate = 2.0f,
                        Sum = 2.0f,
                        Time = 2222
                    }
                }
            };

            var reportJson = new ReportJson {
                TotalBillable = 12345,
                ActivityContainer = new ReportActivityJson {
                    Rows = new List<List<string>> {row1, row2, row3},
                    ZoomLevel = "month"
                },
                Projects = new List<ReportProjectJson> {
                    reportProject1, reportProject2
                },
                TotalCurrencies = new List<ReportCurrencyJson> {
                    new ReportCurrencyJson { Amount = 30, Currency = "euro" },
                    new ReportCurrencyJson { Amount = 300, Currency = "dollar" },
                },
                TotalGrand = 12345
            };

            var reportData = mapper.Map<ReportData> (reportJson);

            Assert.That (reportData.TotalBillable, Is.EqualTo (reportJson.TotalBillable));
            Assert.That (reportData.TotalGrand, Is.EqualTo (reportJson.TotalGrand));
            Assert.That (reportData.Activity.Count, Is.EqualTo (reportJson.ActivityContainer.Rows.Count));
            Assert.That (reportData.Projects.Count, Is.EqualTo (2));
            Assert.That (reportJson.Projects[0].Currencies.Count, Is.EqualTo (reportProject1.Currencies.Count));
        }

        private class PlatformUtils : IPlatformUtils
        {
            public string AppIdentifier { get; set; }

            public string AppVersion { get; set; }

            public bool IsWidgetAvailable { get; set; }

            public ISQLitePlatform SQLiteInfo
            {
                get {
                    return new SQLitePlatformGeneric();
                }
            }

            public void DispatchOnUIThread (Action action)
            {
                action.Invoke();
            }
        }


    }
}
