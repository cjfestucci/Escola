using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusCancelamentoEAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Usuarios",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ativa",
                table: "Turmas",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Fornecedores",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Cancelada",
                table: "ContasReceber",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CanceladaEm",
                table: "ContasReceber",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Cancelada",
                table: "ContasPagar",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CanceladaEm",
                table: "ContasPagar",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Cancelada",
                table: "Cobrancas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CanceladaEm",
                table: "Cobrancas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Alunos",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Ativa",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "Cancelada",
                table: "ContasReceber");

            migrationBuilder.DropColumn(
                name: "CanceladaEm",
                table: "ContasReceber");

            migrationBuilder.DropColumn(
                name: "Cancelada",
                table: "ContasPagar");

            migrationBuilder.DropColumn(
                name: "CanceladaEm",
                table: "ContasPagar");

            migrationBuilder.DropColumn(
                name: "Cancelada",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "CanceladaEm",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Alunos");
        }
    }
}
