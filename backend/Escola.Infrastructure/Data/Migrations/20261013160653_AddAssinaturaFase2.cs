using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssinaturaFase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AcessoAte",
                table: "Assinaturas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AtletasCobrados",
                table: "Assinaturas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvisoSuspensaoEnviadoEm",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LembreteTesteEnviadoEm",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcessoAte",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "AtletasCobrados",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "AvisoSuspensaoEnviadoEm",
                table: "Assinaturas");

            migrationBuilder.DropColumn(
                name: "LembreteTesteEnviadoEm",
                table: "Assinaturas");
        }
    }
}
