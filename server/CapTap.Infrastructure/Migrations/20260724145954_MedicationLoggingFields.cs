using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapTap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MedicationLoggingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TakenAt historically meant "when the dose was confirmed" → LoggedAt.
            migrationBuilder.RenameColumn(
                name: "TakenAt",
                table: "MedicationLogs",
                newName: "LoggedAt");

            migrationBuilder.RenameIndex(
                name: "IX_MedicationLogs_TakenAt",
                table: "MedicationLogs",
                newName: "IX_MedicationLogs_LoggedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDoseTime",
                table: "MedicationLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            // Best-effort backfill: existing logs used TakenAt as the only timestamp.
            migrationBuilder.Sql(
                """
                UPDATE "MedicationLogs"
                SET "ScheduledDoseTime" = "LoggedAt"
                WHERE "ScheduledDoseTime" = TIMESTAMPTZ '0001-01-01 00:00:00+00';
                """);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "MedicationLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_ScheduledDoseTime",
                table: "MedicationLogs",
                column: "ScheduledDoseTime");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_User_Schedule_ScheduledDoseTime",
                table: "MedicationLogs",
                columns: new[] { "UserId", "ScheduleId", "ScheduledDoseTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_ScheduledDoseTime",
                table: "MedicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_MedicationLogs_User_Schedule_ScheduledDoseTime",
                table: "MedicationLogs");

            migrationBuilder.DropColumn(
                name: "ScheduledDoseTime",
                table: "MedicationLogs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "MedicationLogs");

            migrationBuilder.RenameColumn(
                name: "LoggedAt",
                table: "MedicationLogs",
                newName: "TakenAt");

            migrationBuilder.RenameIndex(
                name: "IX_MedicationLogs_LoggedAt",
                table: "MedicationLogs",
                newName: "IX_MedicationLogs_TakenAt");
        }
    }
}
