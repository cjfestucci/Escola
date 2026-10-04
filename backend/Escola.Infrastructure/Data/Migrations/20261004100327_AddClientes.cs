using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Responsaveis_Email",
                table: "Responsaveis");

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Usuarios",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Unidades",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Turmas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "TurmaEducadores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Responsaveis",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "RegistrosRotina",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "RegistrosDiarioClasse",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Produtos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "MovimentacoesEstoque",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "LogsAuditoria",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Jogos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "JogoAtletas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "FotosDiarioClasse",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Fotos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Fornecedores",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "FichasSaude",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "DocumentosSaude",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "ContasReceber",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "ContasPagar",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "ConfiguracoesFinanceiras",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "ConfiguracoesEscola",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Cobrancas",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Campeonatos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "Alunos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "AlunoResponsaveis",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            // Todos os dados que já existiam passam a pertencer a este cliente padrão (o default das colunas ClienteId acima
            // aponta pra ele). Sem esta linha, as chaves estrangeiras criadas a seguir falhariam nas linhas antigas.
            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Nome", "Ativo", "CriadoEm" },
                values: new object[] { new Guid("00000001-0000-0000-0000-000000000001"), "Cliente padrão", true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClienteId",
                table: "Usuarios",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ClienteId_Email",
                table: "Usuarios",
                columns: new[] { "ClienteId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_ClienteId",
                table: "Unidades",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_ClienteId",
                table: "Turmas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_TurmaEducadores_ClienteId",
                table: "TurmaEducadores",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsaveis_ClienteId",
                table: "Responsaveis",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Responsaveis_ClienteId_Email",
                table: "Responsaveis",
                columns: new[] { "ClienteId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosRotina_ClienteId",
                table: "RegistrosRotina",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosDiarioClasse_ClienteId",
                table: "RegistrosDiarioClasse",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_ClienteId",
                table: "Produtos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimentacoesEstoque_ClienteId",
                table: "MovimentacoesEstoque",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_ClienteId",
                table: "LogsAuditoria",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Jogos_ClienteId",
                table: "Jogos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_JogoAtletas_ClienteId",
                table: "JogoAtletas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_FotosDiarioClasse_ClienteId",
                table: "FotosDiarioClasse",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_ClienteId",
                table: "Fotos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Fornecedores_ClienteId",
                table: "Fornecedores",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasSaude_ClienteId",
                table: "FichasSaude",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosSaude_ClienteId",
                table: "DocumentosSaude",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasReceber_ClienteId",
                table: "ContasReceber",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasPagar_ClienteId",
                table: "ContasPagar",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesFinanceiras_ClienteId",
                table: "ConfiguracoesFinanceiras",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracoesEscola_ClienteId",
                table: "ConfiguracoesEscola",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cobrancas_ClienteId",
                table: "Cobrancas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Campeonatos_ClienteId",
                table: "Campeonatos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_ClienteId",
                table: "Alunos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AlunoResponsaveis_ClienteId",
                table: "AlunoResponsaveis",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_AlunoResponsaveis_Clientes_ClienteId",
                table: "AlunoResponsaveis",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Alunos_Clientes_ClienteId",
                table: "Alunos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Campeonatos_Clientes_ClienteId",
                table: "Campeonatos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfiguracoesEscola_Clientes_ClienteId",
                table: "ConfiguracoesEscola",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConfiguracoesFinanceiras_Clientes_ClienteId",
                table: "ConfiguracoesFinanceiras",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContasPagar_Clientes_ClienteId",
                table: "ContasPagar",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ContasReceber_Clientes_ClienteId",
                table: "ContasReceber",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosSaude_Clientes_ClienteId",
                table: "DocumentosSaude",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FichasSaude_Clientes_ClienteId",
                table: "FichasSaude",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fornecedores_Clientes_ClienteId",
                table: "Fornecedores",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fotos_Clientes_ClienteId",
                table: "Fotos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FotosDiarioClasse_Clientes_ClienteId",
                table: "FotosDiarioClasse",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JogoAtletas_Clientes_ClienteId",
                table: "JogoAtletas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Jogos_Clientes_ClienteId",
                table: "Jogos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LogsAuditoria_Clientes_ClienteId",
                table: "LogsAuditoria",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimentacoesEstoque_Clientes_ClienteId",
                table: "MovimentacoesEstoque",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_Clientes_ClienteId",
                table: "Produtos",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosDiarioClasse_Clientes_ClienteId",
                table: "RegistrosDiarioClasse",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RegistrosRotina_Clientes_ClienteId",
                table: "RegistrosRotina",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Responsaveis_Clientes_ClienteId",
                table: "Responsaveis",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TurmaEducadores_Clientes_ClienteId",
                table: "TurmaEducadores",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Turmas_Clientes_ClienteId",
                table: "Turmas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Unidades_Clientes_ClienteId",
                table: "Unidades",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlunoResponsaveis_Clientes_ClienteId",
                table: "AlunoResponsaveis");

            migrationBuilder.DropForeignKey(
                name: "FK_Alunos_Clientes_ClienteId",
                table: "Alunos");

            migrationBuilder.DropForeignKey(
                name: "FK_Campeonatos_Clientes_ClienteId",
                table: "Campeonatos");

            migrationBuilder.DropForeignKey(
                name: "FK_Cobrancas_Clientes_ClienteId",
                table: "Cobrancas");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfiguracoesEscola_Clientes_ClienteId",
                table: "ConfiguracoesEscola");

            migrationBuilder.DropForeignKey(
                name: "FK_ConfiguracoesFinanceiras_Clientes_ClienteId",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropForeignKey(
                name: "FK_ContasPagar_Clientes_ClienteId",
                table: "ContasPagar");

            migrationBuilder.DropForeignKey(
                name: "FK_ContasReceber_Clientes_ClienteId",
                table: "ContasReceber");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosSaude_Clientes_ClienteId",
                table: "DocumentosSaude");

            migrationBuilder.DropForeignKey(
                name: "FK_FichasSaude_Clientes_ClienteId",
                table: "FichasSaude");

            migrationBuilder.DropForeignKey(
                name: "FK_Fornecedores_Clientes_ClienteId",
                table: "Fornecedores");

            migrationBuilder.DropForeignKey(
                name: "FK_Fotos_Clientes_ClienteId",
                table: "Fotos");

            migrationBuilder.DropForeignKey(
                name: "FK_FotosDiarioClasse_Clientes_ClienteId",
                table: "FotosDiarioClasse");

            migrationBuilder.DropForeignKey(
                name: "FK_JogoAtletas_Clientes_ClienteId",
                table: "JogoAtletas");

            migrationBuilder.DropForeignKey(
                name: "FK_Jogos_Clientes_ClienteId",
                table: "Jogos");

            migrationBuilder.DropForeignKey(
                name: "FK_LogsAuditoria_Clientes_ClienteId",
                table: "LogsAuditoria");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimentacoesEstoque_Clientes_ClienteId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_Clientes_ClienteId",
                table: "Produtos");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosDiarioClasse_Clientes_ClienteId",
                table: "RegistrosDiarioClasse");

            migrationBuilder.DropForeignKey(
                name: "FK_RegistrosRotina_Clientes_ClienteId",
                table: "RegistrosRotina");

            migrationBuilder.DropForeignKey(
                name: "FK_Responsaveis_Clientes_ClienteId",
                table: "Responsaveis");

            migrationBuilder.DropForeignKey(
                name: "FK_TurmaEducadores_Clientes_ClienteId",
                table: "TurmaEducadores");

            migrationBuilder.DropForeignKey(
                name: "FK_Turmas_Clientes_ClienteId",
                table: "Turmas");

            migrationBuilder.DropForeignKey(
                name: "FK_Unidades_Clientes_ClienteId",
                table: "Unidades");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ClienteId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_ClienteId_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Unidades_ClienteId",
                table: "Unidades");

            migrationBuilder.DropIndex(
                name: "IX_Turmas_ClienteId",
                table: "Turmas");

            migrationBuilder.DropIndex(
                name: "IX_TurmaEducadores_ClienteId",
                table: "TurmaEducadores");

            migrationBuilder.DropIndex(
                name: "IX_Responsaveis_ClienteId",
                table: "Responsaveis");

            migrationBuilder.DropIndex(
                name: "IX_Responsaveis_ClienteId_Email",
                table: "Responsaveis");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosRotina_ClienteId",
                table: "RegistrosRotina");

            migrationBuilder.DropIndex(
                name: "IX_RegistrosDiarioClasse_ClienteId",
                table: "RegistrosDiarioClasse");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_ClienteId",
                table: "Produtos");

            migrationBuilder.DropIndex(
                name: "IX_MovimentacoesEstoque_ClienteId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropIndex(
                name: "IX_LogsAuditoria_ClienteId",
                table: "LogsAuditoria");

            migrationBuilder.DropIndex(
                name: "IX_Jogos_ClienteId",
                table: "Jogos");

            migrationBuilder.DropIndex(
                name: "IX_JogoAtletas_ClienteId",
                table: "JogoAtletas");

            migrationBuilder.DropIndex(
                name: "IX_FotosDiarioClasse_ClienteId",
                table: "FotosDiarioClasse");

            migrationBuilder.DropIndex(
                name: "IX_Fotos_ClienteId",
                table: "Fotos");

            migrationBuilder.DropIndex(
                name: "IX_Fornecedores_ClienteId",
                table: "Fornecedores");

            migrationBuilder.DropIndex(
                name: "IX_FichasSaude_ClienteId",
                table: "FichasSaude");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosSaude_ClienteId",
                table: "DocumentosSaude");

            migrationBuilder.DropIndex(
                name: "IX_ContasReceber_ClienteId",
                table: "ContasReceber");

            migrationBuilder.DropIndex(
                name: "IX_ContasPagar_ClienteId",
                table: "ContasPagar");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracoesFinanceiras_ClienteId",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropIndex(
                name: "IX_ConfiguracoesEscola_ClienteId",
                table: "ConfiguracoesEscola");

            migrationBuilder.DropIndex(
                name: "IX_Cobrancas_ClienteId",
                table: "Cobrancas");

            migrationBuilder.DropIndex(
                name: "IX_Campeonatos_ClienteId",
                table: "Campeonatos");

            migrationBuilder.DropIndex(
                name: "IX_Alunos_ClienteId",
                table: "Alunos");

            migrationBuilder.DropIndex(
                name: "IX_AlunoResponsaveis_ClienteId",
                table: "AlunoResponsaveis");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Unidades");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "TurmaEducadores");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Responsaveis");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "RegistrosRotina");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "RegistrosDiarioClasse");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "MovimentacoesEstoque");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "LogsAuditoria");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Jogos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "JogoAtletas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "FotosDiarioClasse");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Fotos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Fornecedores");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "FichasSaude");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "DocumentosSaude");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "ContasReceber");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "ContasPagar");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "ConfiguracoesFinanceiras");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "ConfiguracoesEscola");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Cobrancas");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Campeonatos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "Alunos");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "AlunoResponsaveis");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Responsaveis_Email",
                table: "Responsaveis",
                column: "Email",
                unique: true);
        }
    }
}
