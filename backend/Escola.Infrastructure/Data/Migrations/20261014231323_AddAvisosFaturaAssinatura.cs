using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAvisosFaturaAssinatura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvisoAtrasoFaturaId",
                table: "Assinaturas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LembreteFaturaId",
                table: "Assinaturas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReciboFaturaId",
                table: "Assinaturas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvisoAtrasoFaturaId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "LembreteFaturaId",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "ReciboFaturaId",
                table: "Assinaturas");
        }
    }
}
