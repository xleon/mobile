using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Toggl.Phoebe.Data.DataObjects;
using Toggl.Phoebe.Data.Json;
using Toggl.Phoebe.Data.Json.Converters;

namespace Toggl.Phoebe.Tests.Data.Json.Converters
{
    public class TimeEntryJsonConverterTest : Test
    {
        private TimeEntryJsonConverter converter;

        public override void SetUp()
        {
            base.SetUp();

            converter = new TimeEntryJsonConverter();
        }

        [Test]
        public void ExportExisting()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });

                var json = await DataStore.ExecuteInTransactionAsync(ctx => converter.Export(ctx, timeEntryData));
                Assert.AreEqual(2, json.Id);
                Assert.AreEqual("Morning coffee", json.Description);
                Assert.AreEqual(new DateTime(2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual(1, json.WorkspaceId);
                Assert.AreEqual(3, json.UserId);
                Assert.That(json.Tags, Is.Empty);
                Assert.IsNull(json.DeletedAt);
            });
        }

        [Test]
        public void ExportInvalidWorkspaceAndUser()
        {
            TimeEntryData timeEntryData = null;

            RunAsync(async delegate
            {
                timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    ModifiedAt = new DateTime(2014, 1, 3),
                });
            });

            Assert.That(() => RunAsync(async delegate
            {
                await DataStore.ExecuteInTransactionAsync(ctx => converter.Export(ctx, timeEntryData));
            }), Throws.Exception.TypeOf<RelationRemoteIdMissingException> ());
        }

        [Test]
        public void ExportNew()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });

                var json = await DataStore.ExecuteInTransactionAsync(ctx => converter.Export(ctx, timeEntryData));
                Assert.IsNull(json.Id);
                Assert.AreEqual("Morning coffee", json.Description);
                Assert.AreEqual(new DateTime(2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual(1, json.WorkspaceId);
                Assert.AreEqual(3, json.UserId);
                Assert.IsNull(json.DeletedAt);
            });
        }

        [Test]
        public void ExportWithTags()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });
                var tag1Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "mobile",
                    WorkspaceId = workspaceData.Id,
                });
                var tag2Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "on-site",
                    WorkspaceId = workspaceData.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag1Data.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag2Data.Id,
                });

                var json = await DataStore.ExecuteInTransactionAsync(ctx => converter.Export(ctx, timeEntryData));
                Assert.AreEqual(2, json.Id);
                Assert.AreEqual("Morning coffee", json.Description);
                Assert.AreEqual(new DateTime(2014, 1, 3), json.ModifiedAt);
                Assert.AreEqual(1, json.WorkspaceId);
                Assert.AreEqual(3, json.UserId);
                Assert.That(json.Tags, Has.Count.EqualTo(2));
                Assert.That(json.Tags, Has.Exactly(1).Matches<string> (t => t == "mobile"));
                Assert.That(json.Tags, Has.Exactly(1).Matches<string> (t => t == "on-site"));
                Assert.IsNull(json.DeletedAt);
            });
        }

        [Test]
        public void ImportNew()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 3),
                };

                var timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreNotEqual(Guid.Empty, timeEntryData.Id);
                Assert.AreEqual(2, timeEntryData.RemoteId);
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 3), timeEntryData.ModifiedAt);
                Assert.AreEqual(workspaceData.Id, timeEntryData.WorkspaceId);
                Assert.AreEqual(userData.Id, timeEntryData.UserId);
                Assert.IsFalse(timeEntryData.IsDirty);
                Assert.IsFalse(timeEntryData.RemoteRejected);
                Assert.IsNull(timeEntryData.DeletedAt);
            });
        }

        [Test]
        public void ImportUpdated()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 1, 0, DateTimeKind.Utc).ToLocalTime(),   // JSON deserialized to local
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreNotEqual(Guid.Empty, timeEntryData.Id);
                Assert.AreEqual(2, timeEntryData.RemoteId);
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 2, 10, 1, 0, DateTimeKind.Utc), timeEntryData.ModifiedAt);
                Assert.AreEqual(workspaceData.Id, timeEntryData.WorkspaceId);
                Assert.AreEqual(userData.Id, timeEntryData.UserId);
                Assert.IsFalse(timeEntryData.IsDirty);
                Assert.IsFalse(timeEntryData.RemoteRejected);
                Assert.IsNull(timeEntryData.DeletedAt);
            });

            // Warn the user that the test result might be invalid
            if (TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes >= 0)
            {
                Assert.Inconclusive("The test machine timezone should be set to GTM-1 or less to test datetime comparison.");
            }
        }

        [Test]
        public void ImportUpdatedTruncatedStartTime()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 8, 5, 7, 32, 40, DateTimeKind.Utc),
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    StartTime = new DateTime(2014, 8, 5, 7, 32, 40, DateTimeKind.Utc).ToLocalTime(),
                    Duration = -1407223960,
                    ModifiedAt = new DateTime(2014, 8, 5, 9, 9, 33, DateTimeKind.Utc).ToLocalTime(),   // JSON deserialized to local
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreNotEqual(Guid.Empty, timeEntryData.Id);
                Assert.AreEqual(2, timeEntryData.RemoteId);
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 8, 5, 7, 32, 40, DateTimeKind.Utc).Ticks, timeEntryData.StartTime.Ticks);
                Assert.AreEqual(new DateTime(2014, 8, 5, 9, 9, 33, DateTimeKind.Utc), timeEntryData.ModifiedAt);
                Assert.AreEqual(workspaceData.Id, timeEntryData.WorkspaceId);
                Assert.AreEqual(userData.Id, timeEntryData.UserId);
                Assert.IsFalse(timeEntryData.IsDirty);
                Assert.IsFalse(timeEntryData.RemoteRejected);
                Assert.IsNull(timeEntryData.DeletedAt);
            });
        }

        [Test]
        [Description("Overwrite local non-dirty data regardless of the modification times.")]
        public void ImportUpdatedOverwriteNonDirtyLocal()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 2, 9, 59, 0, DateTimeKind.Utc).ToLocalTime(),   // Remote modified is less than local
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 2, 9, 59, 0, DateTimeKind.Utc), timeEntryData.ModifiedAt);
            });
        }

        [Test]
        [Description("Overwrite dirty local data if imported data has a modification time greater than local.")]
        public void ImportUpdatedOverwriteDirtyLocal()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2, 9, 59, 59, DateTimeKind.Utc),
                    IsDirty = true,
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime(),
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), timeEntryData.ModifiedAt);
            });
        }

        [Test]
        [Description("Overwrite local dirty-but-rejected data regardless of the modification times.")]
        public void ImportUpdatedOverwriteRejectedLocal()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 1, 0, DateTimeKind.Utc),
                    IsDirty = true,
                    RemoteRejected = true,
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime(),
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), timeEntryData.ModifiedAt);
            });
        }

        [Test]
        [Description("Keep local dirty data when imported data has same or older modification time.")]
        public void ImportUpdatedKeepDirtyLocal()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    IsDirty = true,
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    UserId = 3,
                    ModifiedAt = new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc).ToLocalTime(),
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreEqual("", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 2, 10, 0, 0, DateTimeKind.Utc), timeEntryData.ModifiedAt);
            });
        }

        [Test]
        public void ImportDefaultUser()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    WorkspaceId = 1,
                    ModifiedAt = new DateTime(2014, 1, 3),
                };

                await SetUpFakeUser(userData.Id);

                var timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreNotEqual(Guid.Empty, timeEntryData.Id);
                Assert.AreEqual(2, timeEntryData.RemoteId);
                Assert.AreEqual("Morning coffee", timeEntryData.Description);
                Assert.AreEqual(new DateTime(2014, 1, 3), timeEntryData.ModifiedAt);
                Assert.AreEqual(workspaceData.Id, timeEntryData.WorkspaceId);
                Assert.AreEqual(userData.Id, timeEntryData.UserId);
                Assert.IsFalse(timeEntryData.IsDirty);
                Assert.IsFalse(timeEntryData.RemoteRejected);
                Assert.IsNull(timeEntryData.DeletedAt);
            });
        }

        [Test]
        public void ImportMissingWorkspaceAndUser()
        {
            RunAsync(async delegate
            {
                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning coffee",
                    ModifiedAt = new DateTime(2014, 1, 3),
                    WorkspaceId = 1,
                    UserId = 2,
                };

                var timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.AreNotEqual(Guid.Empty, timeEntryData.WorkspaceId);

                var workspaceRows = await DataStore.Table<WorkspaceData> ().Where(m => m.Id == timeEntryData.WorkspaceId).ToListAsync();
                var workspaceData = workspaceRows.FirstOrDefault();
                Assert.IsNotNull(workspaceData);
                Assert.IsNotNull(workspaceData.RemoteId);
                Assert.AreEqual(DateTime.MinValue, workspaceData.ModifiedAt);

                var userRows = await DataStore.Table<UserData> ().Where(m => m.Id == timeEntryData.UserId).ToListAsync();
                var userData = userRows.FirstOrDefault();
                Assert.IsNotNull(userData);
                Assert.IsNotNull(userData.RemoteId);
                Assert.AreEqual(DateTime.MinValue, userData.ModifiedAt);
            });
        }

        [Test]
        public void ImportDeleted()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });

                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    DeletedAt = new DateTime(2014, 1, 4),
                };

                var ret = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.IsNull(ret);

                var rows = await DataStore.Table<TimeEntryData> ().Where(m => m.Id == timeEntryData.Id).ToListAsync();
                Assert.That(rows, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void ImportPastDeleted()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });

                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    DeletedAt = new DateTime(2014, 1, 2),
                };

                var ret = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));
                Assert.IsNull(ret);

                var rows = await DataStore.Table<TimeEntryData> ().Where(m => m.Id == timeEntryData.Id).ToListAsync();
                Assert.That(rows, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void ImportNullTags()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });
                var tag1Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "mobile",
                    WorkspaceId = workspaceData.Id,
                });
                var tag2Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "on-site",
                    WorkspaceId = workspaceData.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag1Data.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag2Data.Id,
                });

                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning tea",
                    WorkspaceId = 1,
                    UserId = 3,
                    Tags = null,
                    ModifiedAt = new DateTime(2014, 1, 4),
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));

                var tags = await DataStore.Table<TimeEntryTagData> ().Where(m => m.TimeEntryId == timeEntryData.Id).ToListAsync();
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Exactly(1).Matches<TimeEntryTagData> (t => t.TagId == tag1Data.Id));
                Assert.That(tags, Has.Exactly(1).Matches<TimeEntryTagData> (t => t.TagId == tag2Data.Id));
            });
        }

        [Test]
        public void ImportNewTags()
        {
            RunAsync(async delegate
            {
                var workspaceData = await DataStore.PutAsync(new WorkspaceData()
                {
                    RemoteId = 1,
                    Name = "Test",
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var userData = await DataStore.PutAsync(new UserData()
                {
                    RemoteId = 3,
                    Name = "John",
                    DefaultWorkspaceId = workspaceData.Id,
                    ModifiedAt = new DateTime(2014, 1, 2),
                });
                var timeEntryData = await DataStore.PutAsync(new TimeEntryData()
                {
                    RemoteId = 2,
                    Description = "Morning coffee",
                    WorkspaceId = workspaceData.Id,
                    UserId = userData.Id,
                    ModifiedAt = new DateTime(2014, 1, 3),
                });
                var tag1Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "mobile",
                    WorkspaceId = workspaceData.Id,
                });
                var tag2Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "on-site",
                    WorkspaceId = workspaceData.Id,
                });
                var tag3Data = await DataStore.PutAsync(new TagData()
                {
                    Name = "off-site",
                    WorkspaceId = workspaceData.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag1Data.Id,
                });
                await DataStore.PutAsync(new TimeEntryTagData()
                {
                    TimeEntryId = timeEntryData.Id,
                    TagId = tag2Data.Id,
                });

                var timeEntryJson = new TimeEntryJson()
                {
                    Id = 2,
                    Description = "Morning tea",
                    WorkspaceId = 1,
                    UserId = 3,
                    Tags = new List<string> () { "mobile", "off-site" },
                    ModifiedAt = new DateTime(2014, 1, 4),
                };

                timeEntryData = await DataStore.ExecuteInTransactionAsync(ctx => converter.Import(ctx, timeEntryJson));

                var timeEntryTagRows = await DataStore.Table<TimeEntryTagData> ().Where(m => m.TimeEntryId == timeEntryData.Id).ToListAsync();
                var tags = timeEntryTagRows.Select(r => r.TagId).ToList();
                Assert.That(tags, Has.Count.EqualTo(2));
                Assert.That(tags, Has.Exactly(1).Matches<Guid> (id => id == tag1Data.Id));
                Assert.That(tags, Has.Exactly(1).Matches<Guid> (id => id == tag3Data.Id));
            });
        }
    }
}
