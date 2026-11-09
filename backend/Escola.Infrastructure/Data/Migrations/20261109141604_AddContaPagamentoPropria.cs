using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddContaPagamentoPropria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ConsentimentoEm",
                table: "ContasPagamento",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentimentoIp",
                table: "ContasPagamento",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Propria",
                table: "ContasPagamento",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentimentoEm",
                table: "ContasPagamento");

            migrationBuilder.DropColumn(
                name: "ConsentimentoIp",
                table: "ContasPagamento");

            migrationBuilder.DropColumn(
                name: "Propria",
                table: "ContasPagamento");
        }
    }
}
