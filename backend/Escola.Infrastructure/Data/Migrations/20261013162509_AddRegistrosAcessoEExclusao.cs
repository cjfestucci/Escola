using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrosAcessoEExclusao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvisoExclusaoEnviadoEm",
                table: "Assinaturas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrosAcesso",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Navegador = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OcorridoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAcesso", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAcesso_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAcesso_ClienteId",
                table: "RegistrosAcesso",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAcesso_OcorridoEm",
                table: "RegistrosAcesso",
                column: "OcorridoEm");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAcesso_UsuarioId_OcorridoEm",
                table: "RegistrosAcesso",
                columns: new[] { "UsuarioId", "OcorridoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosAcesso");

            migrationBuilder.DropColumn(
                name: "AvisoExclusaoEnviadoEm",
                table: "Assinaturas");
        }
    }
}
