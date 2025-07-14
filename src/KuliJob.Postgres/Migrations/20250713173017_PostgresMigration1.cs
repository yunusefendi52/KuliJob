using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuliJob.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class PostgresMigration1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cron",
                columns: table => new
                {
                    name = table.Column<string>(type: "text", nullable: false),
                    cron_expression = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "text", nullable: false),
                    timezone = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cron", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "job",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_name = table.Column<string>(type: "text", nullable: false),
                    job_data = table.Column<string>(type: "text", nullable: true),
                    job_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_state = table.Column<int>(type: "integer", nullable: false),
                    start_after = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retry_max_count = table.Column<int>(type: "integer", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    retry_delay_ms = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    queue = table.Column<string>(type: "text", nullable: true),
                    server_name = table.Column<string>(type: "text", nullable: true),
                    throttle_key = table.Column<string>(type: "text", nullable: true),
                    throttle_seconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "job_state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_state = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: true),
                    last_heartbeat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_server", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_job_created_on",
                table: "job",
                column: "created_on");

            migrationBuilder.CreateIndex(
                name: "ix_job_job_name",
                table: "job",
                column: "job_name");

            migrationBuilder.CreateIndex(
                name: "ix_job_start_after",
                table: "job",
                column: "start_after");

            migrationBuilder.CreateIndex(
                name: "ix_job_throttle_key",
                table: "job",
                column: "throttle_key");

            migrationBuilder.CreateIndex(
                name: "ix_job_state_id",
                table: "job_state",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_job_state_id_job_id",
                table: "job_state",
                columns: new[] { "id", "job_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cron");

            migrationBuilder.DropTable(
                name: "job");

            migrationBuilder.DropTable(
                name: "job_state");

            migrationBuilder.DropTable(
                name: "server");
        }
    }
}
