using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAmarelosParaSuspensao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmarelosParaSuspensao",
                table: "Campeonatos",
                type: "integer",
                nullable: true,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmarelosParaSuspensao",
                table: "Campeonatos");
        }
    }
}
