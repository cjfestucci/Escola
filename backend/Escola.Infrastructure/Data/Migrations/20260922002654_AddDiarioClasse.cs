using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiarioClasse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosDiarioClasse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TurmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistradoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosDiarioClasse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosDiarioClasse_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosDiarioClasse_Usuarios_CriadoPorUsuarioId",
                        column: x => x.CriadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FotosDiarioClasse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistroDiarioClasseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotosDiarioClasse", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosDiarioClasse_RegistrosDiarioClasse_RegistroDiarioClass~",
                        column: x => x.RegistroDiarioClasseId,
                        principalTable: "RegistrosDiarioClasse",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosDiarioClasse_RegistroDiarioClasseId",
                table: "FotosDiarioClasse",
                column: "RegistroDiarioClasseId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDiarioClasse_CriadoPorUsuarioId",
                table: "RegistrosDiarioClasse",
                column: "CriadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDiarioClasse_TurmaId_RegistradoEm",
                table: "RegistrosDiarioClasse",
                columns: new[] { "TurmaId", "RegistradoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FotosDiarioClasse");

            migrationBuilder.DropTable(
                name: "RegistrosDiarioClasse");
        }
    }
}
