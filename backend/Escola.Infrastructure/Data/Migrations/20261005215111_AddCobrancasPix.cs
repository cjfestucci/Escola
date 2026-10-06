using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCobrancasPix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UsuarioId",
                table: "LogsAuditoria",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "CobrancasPix",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CobrancaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TxId = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PixCopiaECola = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiraEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UltimaVerificacaoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConcluidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndToEndId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ValorRecebido = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CobrancasPix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CobrancasPix_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CobrancasPix_Cobrancas_CobrancaId",
                        column: x => x.CobrancaId,
                        principalTable: "Cobrancas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CobrancasPix_ClienteId",
                table: "CobrancasPix",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_CobrancasPix_CobrancaId_Status",
                table: "CobrancasPix",
                columns: new[] { "CobrancaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CobrancasPix_Status",
                table: "CobrancasPix",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CobrancasPix_TxId",
                table: "CobrancasPix",
                column: "TxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CobrancasPix");

            migrationBuilder.AlterColumn<Guid>(
                name: "UsuarioId",
                table: "LogsAuditoria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
