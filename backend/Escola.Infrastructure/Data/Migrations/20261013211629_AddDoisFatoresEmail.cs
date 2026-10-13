using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoisFatoresEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoEmailEnviadoEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodigoEmailExpiraEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoEmailHash",
                table: "Usuarios",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DoisFatoresEmailAtivadoEm",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEmailEnviadoEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoEmailExpiraEm",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "CodigoEmailHash",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "DoisFatoresEmailAtivadoEm",
                table: "Usuarios");
        }
    }
}
