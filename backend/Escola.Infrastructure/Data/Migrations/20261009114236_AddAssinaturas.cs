using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssinaturas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assinaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Situacao = table.Column<int>(type: "integer", nullable: false),
                    TesteAte = table.Column<DateOnly>(type: "date", nullable: true),
                    PrimeiroVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorMensal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    AtletasInformados = table.Column<int>(type: "integer", nullable: false),
                    CpfCnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    Cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Celular = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EmailCobranca = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IdClienteGateway = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IdAssinaturaGateway = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LinkPagamento = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VencimentoEmAberto = table.Column<DateOnly>(type: "date", nullable: true),
                    ConviteEnviadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SituacaoAtualizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspensaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanceladaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TermosVersao = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TermosAceitosEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TermosIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TermosNavegador = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assinaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assinaturas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_ClienteId",
                table: "Assinaturas",
                column: "ClienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assinaturas_IdAssinaturaGateway",
                table: "Assinaturas",
                column: "IdAssinaturaGateway");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assinaturas");
        }
    }
}
