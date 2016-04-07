# MIGRATION OF LOCAL DB SCHEMA

The db schema of `unidirectional` is a bit different from
`master`. This means when users with and old version update
the app after integration it will be necessary to migrate
the data in their local databases to the new schema. This
will be done as follows:

- When the SQLite utility class launches (`SyncSqliteDataStore`
  in `unidirectional`) it will detect the version of the local db
- If the version of the db is obsolete, data will be migrated
  in cascade fashion: e.g., if latest version is 5 and db
  version is 3, first data will be migrated to version 4
  and then to 5. This way new versions only need to care
  about migrating data from the immediately previous version.
- A temporary database will be created, data from the old
  db will be read, necessary transformations applied and
  new data will be written to the temporary db.
- When the migration is finished, the old db will be replaced
  by the temporary db. SQLite databases are simple files
  so this will be a normal file system operation.

## DB VERSION

The db version will be written in a special table named
`MetaData` containing key-value pairs (values will be in
JSON format). The current db doesn't include this table.
Because of this, when the version is not found it'll be
considered to 0.

## MAIN CHANGES

The main changes between the old and new schemas are:

- **Misspelling**: `Workspace.RoundingPrecision`
- **CommonData.IsDirty**: Changed to `SyncState`
- **CommonData.RemoteRejected**: Removed
- **WorkspaceData** new properties: `OnlyAdminsMayCreateProjects`,
  `OnlyAdminsSeeBillableRates` and `IsAdmin`
- **UserData** new properties: `GoogleAccessToken` and
  `ApiToken`
- **Remote Ids**: Records contain now both the local and
  **remote** id of related objects to simplify the conversion
  to JSON.
- **TimeEntryTagData**: This data type and the corresponding
  table have been eliminated
- **Tag info in TimeEntryData**: TBD


## DONE

- `MetaData` has been added to `SyncSqliteDataStore`.
- To simplify the reading from the version 0 schema, the old
  models have been included in `unidirectional` in the
  `Toggl.Phoebe.Data.Models.Old.DB_VERSION_0` namespace.
- An `Upgrade` method has been added to the old models.
  This method converts the data type into the new format.

## PENDING

- Implement the process described at the beginning of the
  document: check db version on start, trigger migration
  if old and replacing old db with the new one.
- Add `GoogleAccessToken` and `ApiToken` to `UserData` in
  the new schema. This info must be obtained from platform
  settings.
- New properties of `WorkspaceData`: `OnlyAdminsMayCreateProjects`,
  `OnlyAdminsSeeBillableRates` and `IsAdmin`
- Add **tag info** to `TimeEntryData` in new schema.
- Some properties that were nullable before (`Guid?`) are now
  just Guid (like `TimeEntryData.ProjectId`), decide if
  this change must be extended to other properties,
  particularly remote ids (`TimeEntryData.RemoteProjectId`
  is `long?`).
- If the db contains lots of data the process could take
  some seconds. Consider if a visual cue, like a spinner,
  should be given to prevent the user thinks the apps has frozen.
