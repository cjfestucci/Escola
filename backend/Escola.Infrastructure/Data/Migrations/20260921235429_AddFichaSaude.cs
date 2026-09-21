using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFichaSaude : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FichasSaude",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoSanguineo = table.Column<string>(type: "text", nullable: true),
                    Alergias = table.Column<string>(type: "text", nullable: true),
                    RestricoesAlimentares = table.Column<string>(type: "text", nullable: true),
                    MedicamentosEmUso = table.Column<string>(type: "text", nullable: true),
                    CondicoesSaude = table.Column<string>(type: "text", nullable: true),
                    PlanoSaude = table.Column<string>(type: "text", nullable: true),
                    PediatraNome = table.Column<string>(type: "text", nullable: true),
                    PediatraTelefone = table.Column<string>(type: "text", nullable: true),
                    ContatoEmergenciaNome = table.Column<string>(type: "text", nullable: true),
                    ContatoEmergenciaTelefone = table.Column<string>(type: "text", nullable: true),
                    VacinacaoEmDia = table.Column<bool>(type: "boolean", nullable: false),
                    AutorizaUsoImagem = table.Column<bool>(type: "boolean", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasSaude", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichasSaude_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichasSaude_AlunoId",
                table: "FichasSaude",
                column: "AlunoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FichasSaude");
        }
    }
}
