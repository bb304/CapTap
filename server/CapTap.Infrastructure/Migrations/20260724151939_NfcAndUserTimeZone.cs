using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapTap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NfcAndUserTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NfcTags_Medications_MedicationId",
                table: "NfcTags");

            migrationBuilder.DropIndex(
                name: "IX_NfcTags_MedicationId",
                table: "NfcTags");

            migrationBuilder.RenameColumn(
                name: "LastUsedAt",
                table: "NfcTags",
                newName: "LastScannedAt");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "NfcTags",
                newName: "IsAssigned");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "NfcTags",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                UPDATE "NfcTags" AS t
                SET "UserId" = m."UserId"
                FROM "Medications" AS m
                WHERE m."Id" = t."MedicationId";
                """);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "AuditLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NfcTags_MedicationId_Assigned",
                table: "NfcTags",
                column: "MedicationId",
                unique: true,
                filter: "\"IsAssigned\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_NfcTags_UserId",
                table: "NfcTags",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_NfcTags_Medications_MedicationId",
                table: "NfcTags",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NfcTags_Users_UserId",
                table: "NfcTags",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NfcTags_Medications_MedicationId",
                table: "NfcTags");

            migrationBuilder.DropForeignKey(
                name: "FK_NfcTags_Users_UserId",
                table: "NfcTags");

            migrationBuilder.DropIndex(
                name: "IX_NfcTags_MedicationId_Assigned",
                table: "NfcTags");

            migrationBuilder.DropIndex(
                name: "IX_NfcTags_UserId",
                table: "NfcTags");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "NfcTags");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "LastScannedAt",
                table: "NfcTags",
                newName: "LastUsedAt");

            migrationBuilder.RenameColumn(
                name: "IsAssigned",
                table: "NfcTags",
                newName: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NfcTags_MedicationId",
                table: "NfcTags",
                column: "MedicationId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NfcTags_Medications_MedicationId",
                table: "NfcTags",
                column: "MedicationId",
                principalTable: "Medications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
