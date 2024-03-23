# Configuration
`SierraSam` can be configured by creating a file called `flyway.json` in either the current working directory, or the user profile directory. The configuration file is a JSON file that contains the following properties:

| Property                    | Description                                                                    | Default Value                                       |
|-----------------------------|--------------------------------------------------------------------------------|-----------------------------------------------------|
| `url`                       | The ODBC connection string to the database.                                    | n/a                                                 |
| `user`                      | The username to use when connecting to the database.                           | n/a                                                 |
| `connectionTimeout`         | The timeout to respect when connecting to the database.                        | 15                                                  |
| `connectionRetries`         | The number of retries when connecting to the database.                         | 1                                                   |
| `defaultSchema`             | The database schema to default to.                                             | The default schema if supported by the db provider. |
| `initialiseSql`             | SQL to apply to the database when                                              | n/a                                                 |
| `schemaTable`               | The schema to use when connecting to the database.                             | "flyway_schema_history"                             |
| `locations`                 | A list of directories that contain the migrations.                             | ["filesystem:db/migration"]                         |
| `migrationSuffixes`         | The suffix of the migration files.                                             | [".sql"]                                            |
| `migrationSeparator`        | The characters that separate the version / type from the migration description | "__"                                                |
| `migrationPrefix`           | The prefix of the migration files.                                             | "V"                                                 |
| `installedBy`               | The user that applied the migration.                                           | n/a                                                 |
| `schemas`                   | A list of schemas to use when connecting to the database.                      | []                                                  |
| `repeatableMigrationPrefix` | The prefix of the repeatable migration files.                                  | "R"                                                 |
| `undoMigrationPrefix`       | The prefix of the undo migration files.                                        | "U"                                                 |
| `ignoredMigrations`         | A list of key value pairs for migrations to ignore whilst validating           | ["*:pending"]                                       |
| `initialiseVersion`         | The version to set the schema history table to when initialising the database. | n/a                                                 |
| `output`                    | The output format of commands that return data.                                | "json"                                              |
| `exportDirectory`           | The directory to export the output of commands that return data.               | Current working directory                           |