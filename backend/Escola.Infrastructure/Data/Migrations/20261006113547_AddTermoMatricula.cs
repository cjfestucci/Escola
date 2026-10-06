using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTermoMatricula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MatriculaConfirmadaEm",
                table: "Alunos",
                type: "timestamp with time zone",
                nullable: true);

            // Editado à mão: matrícula que já existia antes do termo continua efetivada (senão sairia da geração de mensalidades e
            // apareceria como "aguardando aceite"). O momento exato não é conhecido; usa o da migration. O termo ainda é pedido no portal.
            migrationBuilder.Sql("UPDATE \"Alunos\" SET \"MatriculaConfirmadaEm\" = NOW() WHERE \"MatriculaConfirmadaEm\" IS NULL;");

            migrationBuilder.CreateTable(
                name: "TermosAceite",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Versao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextoAceito = table.Column<string>(type: "text", nullable: false),
                    AceitoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NavegadorUserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TermosAceite", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TermosAceite_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermosAceite_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermosAceite_Responsaveis_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "Responsaveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TermosAceite_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TermosAceite_AlunoId",
                table: "TermosAceite",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_TermosAceite_ClienteId",
                table: "TermosAceite",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_TermosAceite_ResponsavelId_AlunoId_Versao",
                table: "TermosAceite",
                columns: new[] { "ResponsavelId", "AlunoId", "Versao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TermosAceite_UsuarioId",
                table: "TermosAceite",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TermosAceite");

            migrationBuilder.DropColumn(
                name: "MatriculaConfirmadaEm",
                table: "Alunos");
        }
    }
}
