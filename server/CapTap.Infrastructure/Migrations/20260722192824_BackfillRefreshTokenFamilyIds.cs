using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapTap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillRefreshTokenFamilyIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "RefreshTokens"
                SET "FamilyId" = "Id"
                WHERE "FamilyId" = '00000000-0000-0000-0000-000000000000';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data backfill.
        }
    }
}
