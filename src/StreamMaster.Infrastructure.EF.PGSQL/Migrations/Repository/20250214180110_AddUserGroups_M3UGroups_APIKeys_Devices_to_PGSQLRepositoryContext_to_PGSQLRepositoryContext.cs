using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StreamMaster.Infrastructure.EF.PGSQL.Migrations.Repository
{
    public partial class AddUserGroups_M3UGroups_APIKeys_Devices_to_PGSQLRepositoryContext_to_PGSQLRepositoryContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create APIKeys table if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'APIKeys'
                    ) THEN
                        CREATE TABLE ""APIKeys"" (
                            ""Id"" uuid NOT NULL,
                            ""Key"" text NOT NULL,
                            ""UserId"" text NOT NULL,
                            ""DeviceName"" text NOT NULL,
                            ""Scopes"" text[] NOT NULL,
                            ""Expiration"" timestamp with time zone,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""LastUsedAt"" timestamp with time zone,
                            ""IsActive"" boolean NOT NULL,
                            CONSTRAINT ""PK_APIKeys"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            // Create Devices table if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'Devices'
                    ) THEN
                        CREATE TABLE ""Devices"" (
                            ""Id"" uuid NOT NULL,
                            ""ApiKeyId"" text NOT NULL,
                            ""UserId"" text NOT NULL,
                            ""DeviceType"" text NOT NULL,
                            ""DeviceId"" text NOT NULL,
                            ""UserAgent"" text NOT NULL,
                            ""IPAddress"" text NOT NULL,
                            ""LastActivity"" timestamp with time zone NOT NULL,
                            CONSTRAINT ""PK_Devices"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            // Create M3UGroups table if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'M3UGroups'
                    ) THEN
                        CREATE TABLE ""M3UGroups"" (
                            ""Id"" integer NOT NULL GENERATED ALWAYS AS IDENTITY,
                            ""Name"" citext NOT NULL,
                            ""IsIncluded"" boolean NOT NULL,
                            ""TotalCount"" integer NOT NULL,
                            ""IsUser"" boolean NOT NULL,
                            ""IsPPV"" boolean NOT NULL,
                            ""IsVOD"" boolean NOT NULL,
                            ""M3UFileId"" integer NOT NULL,
                            CONSTRAINT ""PK_M3UGroups"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            // Create UserGroups table if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_name = 'UserGroups'
                    ) THEN
                        CREATE TABLE ""UserGroups"" (
                            ""Id"" integer NOT NULL GENERATED ALWAYS AS IDENTITY,
                            ""TotalCount"" integer NOT NULL,
                            ""Name"" citext NOT NULL,
                            CONSTRAINT ""PK_UserGroups"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APIKeys");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "M3UGroups");

            migrationBuilder.DropTable(
                name: "UserGroups");
        }
    }
}