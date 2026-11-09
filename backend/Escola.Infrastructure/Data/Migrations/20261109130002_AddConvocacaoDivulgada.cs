using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConvocacaoDivulgada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConvocacaoAlteradaEm",
                table: "Jogos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvocacaoDivulgadaEm",
                table: "Jogos",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConvocacaoAlteradaEm",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "ConvocacaoDivulgadaEm",
                table: "Jogos");
        }
    }
}
