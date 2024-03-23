# SierraSam
[![SierraSam](https://github.com/georgeamason/SierraSam/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/georgeamason/SierraSam/actions/workflows/ci.yml)

SierraSam is a C# port of flyway.

### Information
The `info` command is used to display the current state of the database, as well as any migrations that have been discovered.

```json
[
   {
      "MigrationType": "Versioned",
      "Version": "1",
      "Description": "Create_employee_table",
      "Type": "SQL",
      "Checksum": "2c9b07de22e661f27582615e00f10553",
      "InstalledOn": "2024-01-15T23:00:28.587Z",
      "State": "Applied"
   }
]
```

### Validate
The `validate` command is used to validate the checksums of applied migrations against the checksums of discovered migrations. If any checksums do not match, the command will fail and display the offending migrations.

The 'ignoredMigrations' configuration parameter can be used to alter the behaviour of this command. By default, `pending` state migrations are ignored. Any discovered migrations  that have not been applied to the database passed validation.

| State     | Description                                                       |
|-----------|-------------------------------------------------------------------|
| `pending` | Discovered migrations that have not been applied to the database. |
| `applied` | Applied migrations that have been discovered.                     |
| `missing` | Applied migrations that have not been discovered.                 |

### Rollup
The `rollup` command is used to rollup all the migrations into a single file. This is useful for creating a single file that can be used to create a database from scratch. The `rollup` command will create a file called `rollup.sql` in the current directory.

### Repair
The `repair` command is used to realign the applied migrations with those that have been discovered locally. This is useful if the schema history table has been modified or corrupted. Additionally, if a locally discovered migration has been altered in any way, perhaps some reformatting, any further migrations will fail validation. The `repair` command will update the checksum of any applied migrations to match the checksum of the discovered migration.

> Precaution should be taken when using this command. It's possible that the schema history table can fall out of sync with the actual state of the database. For example, if a migration is applied using `migrate`, then significantly altered locally before finally being repaired, the checksum represented in the schema history table isn't what was actually applied to the database.