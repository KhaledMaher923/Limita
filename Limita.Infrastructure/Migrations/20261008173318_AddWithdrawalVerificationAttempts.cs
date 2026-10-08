using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Limita.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalVerificationAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedVerificationAttempts",
                table: "Withdrawals",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedVerificationAttempts",
                table: "Withdrawals");
        }
    }
}
