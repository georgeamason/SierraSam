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

### Rollup
The `rollup` command is used to rollup all the migrations into a single file. This is useful for creating a single file that can be used to create a database from scratch. The `rollup` command will create a file called `rollup.sql` in the current directory.
