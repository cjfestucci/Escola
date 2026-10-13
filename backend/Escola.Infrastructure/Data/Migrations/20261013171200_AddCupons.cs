using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCupons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CupomCodigo",
                table: "Assinaturas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CupomMeses",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CupomPercentual",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PercentualDesconto = table.Column<int>(type: "integer", nullable: false),
                    Meses = table.Column<int>(type: "integer", nullable: true),
                    ValidoAte = table.Column<DateOnly>(type: "date", nullable: true),
                    UsosMaximos = table.Column<int>(type: "integer", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cupons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cupons_Codigo",
                table: "Cupons",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cupons");

            migrationBuilder.DropColumn(
                name: "CupomCodigo",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "CupomMeses",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "CupomPercentual",
                table: "Assinaturas");
        }
    }
}
