using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPendenciaPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Pendencia",
                table: "CobrancasPix",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendenciaConferidaEm",
                table: "CobrancasPix",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CobrancasPix_Pendencia",
                table: "CobrancasPix",
                column: "Pendencia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CobrancasPix_Pendencia",
                table: "CobrancasPix");

            migrationBuilder.DropColumn(
                name: "Pendencia",
                table: "CobrancasPix");

            migrationBuilder.DropColumn(
                name: "PendenciaConferidaEm",
                table: "CobrancasPix");
        }
    }
}
