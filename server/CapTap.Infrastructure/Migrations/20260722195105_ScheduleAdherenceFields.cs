using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapTap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ScheduleAdherenceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "MedicationSchedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "MedicationSchedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MedicationSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationSchedules_MedicationId_ScheduledTime",
                table: "MedicationSchedules",
                columns: new[] { "MedicationId", "ScheduledTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationSchedules_MedicationId_ScheduledTime",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "MedicationSchedules");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MedicationSchedules");
        }
    }
}
