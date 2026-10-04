using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompeticoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campeonatos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campeonatos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jogos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampeonatoId = table.Column<Guid>(type: "uuid", nullable: true),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Adversario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    Local = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Mando = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GolsPro = table.Column<int>(type: "integer", nullable: true),
                    GolsContra = table.Column<int>(type: "integer", nullable: true),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jogos_Campeonatos_CampeonatoId",
                        column: x => x.CampeonatoId,
                        principalTable: "Campeonatos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Jogos_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JogoAtletas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JogoId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Titular = table.Column<bool>(type: "boolean", nullable: false),
                    Gols = table.Column<int>(type: "integer", nullable: false),
                    CartoesAmarelos = table.Column<int>(type: "integer", nullable: false),
                    CartaoVermelho = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JogoAtletas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JogoAtletas_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JogoAtletas_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JogoAtletas_AlunoId",
                table: "JogoAtletas",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_JogoAtletas_JogoId_AlunoId",
                table: "JogoAtletas",
                columns: new[] { "JogoId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_CampeonatoId",
                table: "Jogos",
                column: "CampeonatoId");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_Data",
                table: "Jogos",
                column: "Data");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_TurmaId",
                table: "Jogos",
                column: "TurmaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JogoAtletas");

            migrationBuilder.DropTable(
                name: "Jogos");

            migrationBuilder.DropTable(
                name: "Campeonatos");
        }
    }
}
