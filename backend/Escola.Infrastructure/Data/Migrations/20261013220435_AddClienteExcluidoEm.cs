using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteExcluidoEm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExcluidoEm",
                table: "Clientes",
                type: "timestamp with time zone",
                nullable: true);

            // Editado à mão: clientes que já tiveram os dados excluídos (anonimizados pela ExclusaoDadosCliente) ganham a data, senão o
            // provisionamento do Suporte continuaria recriando a conta neles. A data exata da exclusão não existe — usa o momento da migration.
            migrationBuilder.Sql("UPDATE \"Clientes\" SET \"ExcluidoEm\" = NOW() WHERE \"Nome\" = 'Cliente excluído' AND NOT \"Ativo\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcluidoEm",
                table: "Clientes");
        }
    }
}
