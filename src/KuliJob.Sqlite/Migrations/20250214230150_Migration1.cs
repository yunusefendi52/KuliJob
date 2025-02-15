using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KuliJob.Sqlite.Migrations
{
    public partial class Migration1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cron",
                columns: table => new
                {
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    cron_expression = table.Column<string>(type: "TEXT", nullable: false),
                    data = table.Column<string>(type: "TEXT", nullable: false),
                    timezone = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<long>(type: "INTEGER", nullable: false),
                    updated_at = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cron", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "job",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    job_name = table.Column<string>(type: "TEXT", nullable: false),
                    job_data = table.Column<string>(type: "TEXT", nullable: false),
                    job_state = table.Column<int>(type: "INTEGER", nullable: false),
                    start_after = table.Column<long>(type: "INTEGER", nullable: false),
                    started_on = table.Column<long>(type: "INTEGER", nullable: true),
                    completed_on = table.Column<long>(type: "INTEGER", nullable: true),
                    cancelled_on = table.Column<long>(type: "INTEGER", nullable: true),
                    failed_on = table.Column<long>(type: "INTEGER", nullable: true),
                    failed_message = table.Column<string>(type: "TEXT", nullable: true),
                    created_on = table.Column<long>(type: "INTEGER", nullable: false),
                    retry_max_count = table.Column<int>(type: "INTEGER", nullable: false),
                    retry_count = table.Column<int>(type: "INTEGER", nullable: false),
                    retry_delay_ms = table.Column<int>(type: "INTEGER", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false),
                    queue = table.Column<string>(type: "TEXT", nullable: true),
                    server_name = table.Column<string>(type: "TEXT", nullable: true),
                    throttle_key = table.Column<string>(type: "TEXT", nullable: true),
                    throttle_seconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_job", x => x.id);
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
                name: "ix_job_job_state",
                table: "job",
                column: "job_state");

            migrationBuilder.CreateIndex(
                name: "ix_job_start_after",
                table: "job",
                column: "start_after");

            migrationBuilder.CreateIndex(
                name: "ix_job_throttle_key",
                table: "job",
                column: "throttle_key");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cron");

            migrationBuilder.DropTable(
                name: "job");
        }
    }
}
