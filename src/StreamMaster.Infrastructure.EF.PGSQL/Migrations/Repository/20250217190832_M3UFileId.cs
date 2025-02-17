using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamMaster.Infrastructure.EF.PGSQL.Migrations.Repository
{
    /// <inheritdoc />
    public partial class M3UFileId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SMChannelStreamLinks",
                table: "SMChannelStreamLinks");

            migrationBuilder.DropColumn(
                name: "M3UFileId",
                table: "SMChannels");

            migrationBuilder.AddColumn<int>(
                name: "SMStreamM3UFileId",
                table: "SMChannelStreamLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SMChannelStreamLinks",
                table: "SMChannelStreamLinks",
                columns: new[] { "SMChannelId", "SMStreamId", "SMStreamM3UFileId" });
        }

        /// <inheritdoc />
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
