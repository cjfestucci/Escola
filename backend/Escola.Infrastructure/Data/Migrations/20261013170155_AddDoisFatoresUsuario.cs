using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoisFatoresUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigosRecuperacaoHash",
                table: "Usuarios",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TotpAtivadoEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSegredoCriptografado",
                table: "Usuarios",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSegredoPendenteCriptografado",
                table: "Usuarios",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigosRecuperacaoHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TotpAtivadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TotpSegredoCriptografado",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TotpSegredoPendenteCriptografado",
                table: "Usuarios");
        }
    }
}
