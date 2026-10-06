using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMensalidadesEmLote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorMensalidade",
                table: "Turmas",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaVencimentoMensalidade",
                table: "ConfiguracoesFinanceiras",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<decimal>(
                name: "JurosMensaisPercentual",
                table: "ConfiguracoesFinanceiras",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MultaAtrasoPercentual",
                table: "ConfiguracoesFinanceiras",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "Competencia",
                table: "Cobrancas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPago",
                table: "Cobrancas",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DescontoMensalidadePercentual",
                table: "Alunos",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDesconto",
                table: "Alunos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cobrancas_AlunoId_Competencia",
                table: "Cobrancas",
                columns: new[] { "AlunoId", "Competencia" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cobrancas_AlunoId_Competencia",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "ValorMensalidade",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "DiaVencimentoMensalidade",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "JurosMensaisPercentual",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "MultaAtrasoPercentual",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "Competencia",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "ValorPago",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "DescontoMensalidadePercentual",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "MotivoDesconto",
                table: "Alunos");
        }
    }
}
