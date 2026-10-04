using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCorPrincipalConfiguracaoEscola : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorPrincipal",
                table: "ConfiguracoesEscola",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorPrincipal",
                table: "ConfiguracoesEscola");
        }
    }
}
