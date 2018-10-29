using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Swarm.Migrator.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SWARM_CLIENTS",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NAME = table.Column<string>(maxLength: 120, nullable: false),
                    GROUP = table.Column<string>(maxLength: 120, nullable: true),
                    CONNECTION_ID = table.Column<string>(maxLength: 50, nullable: false),
                    IP = table.Column<string>(maxLength: 50, nullable: false),
                    IS_CONNECTED = table.Column<bool>(nullable: false),
                    CREATION_TIME = table.Column<DateTimeOffset>(nullable: false),
                    LAST_MODIFICATION_TIME = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SWARM_CLIENTS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SWARM_JOB_PROPERTIES",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    JOB_ID = table.Column<string>(maxLength: 32, nullable: true),
                    NAME = table.Column<string>(maxLength: 32, nullable: true),
                    VALUE = table.Column<string>(maxLength: 250, nullable: true),
                    CREATION_TIME = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SWARM_JOB_PROPERTIES", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SWARM_JOB_STATE",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    JOB_ID = table.Column<string>(maxLength: 32, nullable: true),
                    TRACE_ID = table.Column<string>(maxLength: 32, nullable: true),
                    STATE = table.Column<int>(nullable: false),
                    CLIENT = table.Column<string>(maxLength: 120, nullable: true),
                    MSG = table.Column<string>(maxLength: 500, nullable: true),
                    SHARDING = table.Column<int>(nullable: false),
                    CREATION_TIME = table.Column<DateTimeOffset>(nullable: false),
                    LAST_MODIFICATION_TIME = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SWARM_JOB_STATE", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SWARM_JOBS",
                columns: table => new
                {
                    ID = table.Column<string>(maxLength: 32, nullable: false),
                    STATE = table.Column<int>(nullable: false),
                    TRIGGER = table.Column<int>(nullable: false),
                    PERFORMER = table.Column<int>(nullable: false),
                    EXECUTER = table.Column<int>(nullable: false),
                    NAME = table.Column<string>(maxLength: 120, nullable: false),
                    GROUP = table.Column<string>(maxLength: 120, nullable: false),
                    LOAD = table.Column<int>(nullable: false),
                    SHARDING = table.Column<int>(nullable: false),
                    SHARDING_PARAMETERS = table.Column<string>(maxLength: 500, nullable: true),
                    DESCRIPTION = table.Column<string>(maxLength: 500, nullable: true),
                    RETRY_COUNT = table.Column<int>(nullable: false),
                    OWNER = table.Column<string>(maxLength: 120, nullable: true),
                    CONCURRENT_EXECUTION_DISALLOWED = table.Column<bool>(nullable: false),
                    CREATION_TIME = table.Column<DateTimeOffset>(nullable: false),
                    LAST_MODIFICATION_TIME = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SWARM_JOBS", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SWARM_LOGS",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    JOB_ID = table.Column<string>(maxLength: 32, nullable: true),
                    TRACE_ID = table.Column<string>(maxLength: 32, nullable: true),
                    MSG = table.Column<string>(nullable: true),
                    CREATION_TIME = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SWARM_LOGS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_CLIENTS_CONNECTION_ID",
                table: "SWARM_CLIENTS",
                column: "CONNECTION_ID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_CLIENTS_CREATION_TIME",
                table: "SWARM_CLIENTS",
                column: "CREATION_TIME");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_CLIENTS_NAME_GROUP",
                table: "SWARM_CLIENTS",
                columns: new[] { "NAME", "GROUP" },
                unique: true,
                filter: "[GROUP] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_PROPERTIES_JOB_ID",
                table: "SWARM_JOB_PROPERTIES",
                column: "JOB_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_PROPERTIES_JOB_ID_NAME",
                table: "SWARM_JOB_PROPERTIES",
                columns: new[] { "JOB_ID", "NAME" },
                unique: true,
                filter: "[JOB_ID] IS NOT NULL AND [NAME] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_STATE_CREATION_TIME",
                table: "SWARM_JOB_STATE",
                column: "CREATION_TIME");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_STATE_JOB_ID",
                table: "SWARM_JOB_STATE",
                column: "JOB_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_STATE_JOB_ID_TRACE_ID",
                table: "SWARM_JOB_STATE",
                columns: new[] { "JOB_ID", "TRACE_ID" });

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_STATE_SHARDING_TRACE_ID_CLIENT",
                table: "SWARM_JOB_STATE",
                columns: new[] { "SHARDING", "TRACE_ID", "CLIENT" },
                unique: true,
                filter: "[TRACE_ID] IS NOT NULL AND [CLIENT] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOB_STATE_SHARDING_JOB_ID_TRACE_ID_CLIENT",
                table: "SWARM_JOB_STATE",
                columns: new[] { "SHARDING", "JOB_ID", "TRACE_ID", "CLIENT" },
                unique: true,
                filter: "[JOB_ID] IS NOT NULL AND [TRACE_ID] IS NOT NULL AND [CLIENT] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOBS_CREATION_TIME",
                table: "SWARM_JOBS",
                column: "CREATION_TIME");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOBS_GROUP",
                table: "SWARM_JOBS",
                column: "GROUP");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOBS_NAME",
                table: "SWARM_JOBS",
                column: "NAME");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOBS_OWNER",
                table: "SWARM_JOBS",
                column: "OWNER");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_JOBS_NAME_GROUP",
                table: "SWARM_JOBS",
                columns: new[] { "NAME", "GROUP" });

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_LOGS_CREATION_TIME",
                table: "SWARM_LOGS",
                column: "CREATION_TIME");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_LOGS_JOB_ID",
                table: "SWARM_LOGS",
                column: "JOB_ID");

            migrationBuilder.CreateIndex(
                name: "IX_SWARM_LOGS_JOB_ID_TRACE_ID",
                table: "SWARM_LOGS",
                columns: new[] { "JOB_ID", "TRACE_ID" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SWARM_CLIENTS");

            migrationBuilder.DropTable(
                name: "SWARM_JOB_PROPERTIES");

            migrationBuilder.DropTable(
                name: "SWARM_JOB_STATE");

            migrationBuilder.DropTable(
                name: "SWARM_JOBS");

            migrationBuilder.DropTable(
                name: "SWARM_LOGS");
        }
    }
}
