using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadeIdToTurma : Migration
    {
        // Toda Turma passou a exigir UnidadeId — turmas que já existiam no banco não tinham
        // unidade nenhuma, então esta migration cria uma "Unidade Principal" e migra todas
        // as turmas existentes pra ela antes de tornar a coluna obrigatória.
        private static readonly Guid UnidadePrincipalId = new("11111111-1111-1111-1111-111111111111");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Unidades",
                columns: new[] { "Id", "Nome", "Endereco", "Telefone", "Ativa" },
                values: new object[] { UnidadePrincipalId, "Unidade Principal", null, null, true });

            migrationBuilder.AddColumn<Guid>(
                name: "UnidadeId",
                table: "Turmas",
                type: "uuid",
                nullable: false,
                defaultValue: UnidadePrincipalId);

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_UnidadeId",
                table: "Turmas",
                column: "UnidadeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Unidades_UnidadeId",
                table: "Turmas",
                column: "UnidadeId",
                principalTable: "Unidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Unidades_UnidadeId",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_UnidadeId",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "UnidadeId",
                table: "Turmas");

            migrationBuilder.DeleteData(
                table: "Unidades",
                keyColumn: "Id",
                keyValue: UnidadePrincipalId);
        }
    }
}
