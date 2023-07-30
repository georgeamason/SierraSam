﻿using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;

namespace SierraSam.Core.Tests.Unit;

[TestFixture]
internal sealed class PendingMigrationTests
{
    [TestCase("./RRR2.2.2223__Add_new_table.sql", "RRR", "2.2.2223", "__", "Add_new_table")]
    [TestCase("./V2__Desc.sql", "V", "2", "__", "Desc")]
    [TestCase("./VV22.12.2@@xyz.sql", "VV", "22.12.2", "@@", "xyz")]
    [TestCase("./U1.1__Fix_indexes.sql", "U", "1.1", "__", "Fix_indexes")]
    [TestCase("./V2__Add a new table.sql", "V", "2", "__", "Add a new table")]
    [TestCase("./R__My_view.sql", "R", null, "__", "My_view")]
    [TestCase("./V1004__make_v11_sql_monitor_license.sql", "V", "1004", "__", "make_v11_sql_monitor_license")]
    [TestCase("./MIG1__description is here.sql", "MIG", "1", "__", "description is here")]
    [TestCase("./V2023.01.12.4343__create_users_table.sql", "V", "2023.01.12.4343", "__", "create_users_table")]
    [TestCase("./Repp__desc.sql", "Repp", null, "__", "desc")]
    [TestCase("./Verr2__Desc.sql", "Verr", "2", "__", "Desc")]
    [TestCase("./VV1_2_3@@xyz.sql", "VV", null, "@@", "xyz")]
    [TestCase("./VV1_2_3@@@xyz.sql", "VV", null, "@@@", "xyz")]
    [TestCase("./V1003__delete-invalid-license.sql", "V", "1003", "__", null)]
    [TestCase("./V1016__remove-sql-prompt-25-price-break.sql", "V", "1016", "__", null)]
    public void Properties_return_expected_result
        (string filePath, string prefix, string version, string separator, string description)
    {
        var mockFileSystem = new MockFileSystem();

        mockFileSystem.AddFile(filePath, new MockFileData("SQL"));

        var mockFileInfo = new MockFileInfo(mockFileSystem, filePath);

        var configuration = new Configuration
            (migrationPrefix: prefix,
             migrationSeparator: separator);

        var migration = PendingMigration.Parse(configuration, mockFileInfo);

        migration.Version.Should().Be(version);
        migration.Description.Should().Be(description);
    }
}