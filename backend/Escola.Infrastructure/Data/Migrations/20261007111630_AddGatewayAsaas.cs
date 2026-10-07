using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGatewayAsaas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "Responsaveis",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdClienteAsaas",
                table: "Responsaveis",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provedor",
                table: "CobrancasPix",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ContasPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provedor = table.Column<int>(type: "integer", nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IdExterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WalletId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiKeyCriptografada = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TitularNome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TitularCpfCnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    TitularEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ChavePixCriada = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookConfigurado = table.Column<bool>(type: "boolean", nullable: false),
                    SituacaoGateway = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SituacaoConsultadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasPagamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasPagamento_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagamento_ClienteId",
                table: "ContasPagamento",
                column: "ClienteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContasPagamento");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "Responsaveis");

            migrationBuilder.DropColumn(
                name: "IdClienteAsaas",
                table: "Responsaveis");

            migrationBuilder.DropColumn(
                name: "Provedor",
                table: "CobrancasPix");
        }
    }
}
