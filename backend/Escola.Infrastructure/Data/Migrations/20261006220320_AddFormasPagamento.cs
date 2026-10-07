using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFormasPagamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstrucoesPagamentoPresencial",
                table: "ConfiguracoesFinanceiras",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PagamentoBoletoAtivo",
                table: "ConfiguracoesFinanceiras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PagamentoPixAtivo",
                table: "ConfiguracoesFinanceiras",
                type: "boolean",
                nullable: false,
                // Editado à mão: o EF gerou false, o que desligaria o Pix de todas as escolas que já existem. Pix era a única forma
                // de pagamento antes desta opção, então continua ligado.
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PagamentoPresencialAtivo",
                table: "ConfiguracoesFinanceiras",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstrucoesPagamentoPresencial",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "PagamentoBoletoAtivo",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "PagamentoPixAtivo",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "PagamentoPresencialAtivo",
                table: "ConfiguracoesFinanceiras");
        }
    }
}
