using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamMaster.Infrastructure.EF.PGSQL.Migrations.Repository
{
    public partial class M3UFileId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First check if M3UFileId exists
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.columns
                        WHERE table_name = 'SMChannels'
                        AND column_name = 'M3UFileId'
                    ) THEN
                        ALTER TABLE ""SMChannels"" DROP COLUMN ""M3UFileId"";
                    END IF;
                END $$;
            ");

            // Drop the existing primary key
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.table_constraints
                        WHERE constraint_name = 'PK_SMChannelStreamLinks'
                    ) THEN
                        ALTER TABLE ""SMChannelStreamLinks"" DROP CONSTRAINT ""PK_SMChannelStreamLinks"";
                    END IF;
                END $$;
            ");

            // Add new column if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.columns
                        WHERE table_name = 'SMChannelStreamLinks'
                        AND column_name = 'SMStreamM3UFileId'
                    ) THEN
                        ALTER TABLE ""SMChannelStreamLinks""
                        ADD COLUMN ""SMStreamM3UFileId"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");

            // Add new primary key
            migrationBuilder.Sql(@"
                ALTER TABLE ""SMChannelStreamLinks""
                ADD CONSTRAINT ""PK_SMChannelStreamLinks""
                PRIMARY KEY (""SMChannelId"", ""SMStreamId"", ""SMStreamM3UFileId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SMChannelStreamLinks",
                table: "SMChannelStreamLinks");

            migrationBuilder.DropColumn(
                name: "SMStreamM3UFileId",
                table: "SMChannelStreamLinks");

            migrationBuilder.AddColumn<int>(
                name: "M3UFileId",
                table: "SMChannels",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SMChannelStreamLinks",
                table: "SMChannelStreamLinks",
                columns: new[] { "SMChannelId", "SMStreamId" });
        }
    }
}