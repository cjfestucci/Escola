using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMesesGratisCupom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MesesGratis",
                table: "Cupons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CupomMesesGratis",
                table: "Assinaturas",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MesesGratis",
                table: "Cupons");

            migrationBuilder.DropColumn(
                name: "CupomMesesGratis",
                table: "Assinaturas");
        }
    }
}
