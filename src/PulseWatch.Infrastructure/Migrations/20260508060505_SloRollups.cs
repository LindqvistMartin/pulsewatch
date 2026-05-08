using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWatch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SloRollups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProbeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AutoDetected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidents_Probes_ProbeId",
                        column: x => x.ProbeId,
                        principalTable: "Probes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SloDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProbeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAvailabilityPct = table.Column<double>(type: "double precision", nullable: false),
                    TargetLatencyP95Ms = table.Column<int>(type: "integer", nullable: true),
                    WindowDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SloDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SloDefinitions_Probes_ProbeId",
                        column: x => x.ProbeId,
                        principalTable: "Probes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentUpdates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentUpdates_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SloMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SloDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailabilityPct = table.Column<double>(type: "double precision", nullable: false),
                    P95LatencyMs = table.Column<int>(type: "integer", nullable: true),
                    ErrorBudgetTotalSeconds = table.Column<double>(type: "double precision", nullable: false),
                    ErrorBudgetConsumedSeconds = table.Column<double>(type: "double precision", nullable: false),
                    BurnRate = table.Column<double>(type: "double precision", nullable: false),
                    ProjectedExhaustionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SloMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SloMeasurements_SloDefinitions_SloDefinitionId",
                        column: x => x.SloDefinitionId,
                        principalTable: "SloDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ProbeId_ClosedAt",
                table: "Incidents",
                columns: new[] { "ProbeId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentUpdates_IncidentId",
                table: "IncidentUpdates",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SloDefinitions_ProbeId",
                table: "SloDefinitions",
                column: "ProbeId");

            migrationBuilder.CreateIndex(
                name: "IX_SloMeasurements_SloDefinitionId_ComputedAt",
                table: "SloMeasurements",
                columns: new[] { "SloDefinitionId", "ComputedAt" });

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW health_check_1m AS
                SELECT
                    "ProbeId" AS probe_id,
                    date_trunc('minute', "CheckedAt") AS bucket,
                    count(*) AS total,
                    count(*) FILTER (WHERE "IsSuccess") AS success,
                    avg("ResponseTimeMs")::int AS avg_ms,
                    percentile_cont(0.5) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p50_ms,
                    percentile_cont(0.95) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p95_ms,
                    percentile_cont(0.99) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p99_ms
                FROM "HealthChecks"
                WHERE "CheckedAt" >= now() - interval '7 days'
                GROUP BY "ProbeId", bucket;
                CREATE UNIQUE INDEX ON health_check_1m(probe_id, bucket);
                """);

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW health_check_1h AS
                SELECT
                    "ProbeId" AS probe_id,
                    date_trunc('hour', "CheckedAt") AS bucket,
                    count(*) AS total,
                    count(*) FILTER (WHERE "IsSuccess") AS success,
                    avg("ResponseTimeMs")::int AS avg_ms,
                    percentile_cont(0.5) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p50_ms,
                    percentile_cont(0.95) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p95_ms,
                    percentile_cont(0.99) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p99_ms
                FROM "HealthChecks"
                WHERE "CheckedAt" >= now() - interval '30 days'
                GROUP BY "ProbeId", bucket;
                CREATE UNIQUE INDEX ON health_check_1h(probe_id, bucket);
                """);

            migrationBuilder.Sql("""
                CREATE MATERIALIZED VIEW health_check_1d AS
                SELECT
                    "ProbeId" AS probe_id,
                    date_trunc('day', "CheckedAt") AS bucket,
                    count(*) AS total,
                    count(*) FILTER (WHERE "IsSuccess") AS success,
                    avg("ResponseTimeMs")::int AS avg_ms,
                    percentile_cont(0.5) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p50_ms,
                    percentile_cont(0.95) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p95_ms,
                    percentile_cont(0.99) WITHIN GROUP (ORDER BY "ResponseTimeMs")::int AS p99_ms
                FROM "HealthChecks"
                WHERE "CheckedAt" >= now() - interval '365 days'
                GROUP BY "ProbeId", bucket;
                CREATE UNIQUE INDEX ON health_check_1d(probe_id, bucket);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS health_check_1d;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS health_check_1h;");
            migrationBuilder.Sql("DROP MATERIALIZED VIEW IF EXISTS health_check_1m;");

            migrationBuilder.DropTable(
                name: "IncidentUpdates");

            migrationBuilder.DropTable(
                name: "SloMeasurements");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "SloDefinitions");
        }
    }
}
