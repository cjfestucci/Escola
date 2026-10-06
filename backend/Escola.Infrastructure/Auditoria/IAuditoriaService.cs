using Escola.Domain.Enums;

namespace Escola.Infrastructure.Auditoria;

/// <summary>Grava uma entrada na trilha de auditoria (quem criou/editou/excluiu o quê).
/// Não chama SaveChangesAsync — a entrada entra no mesmo SaveChanges da operação que a originou,
/// pra log e mudança de dado serem atômicos (ou os dois persistem, ou nenhum).</summary>
public interface IAuditoriaService
{
    /// <param name="turmaId">Turma dona do registro auditado, só quando a entidade pertence a uma turma
    /// (ex.: RegistroDiarioClasse) — habilita consultar o histórico por turma+dia mesmo após exclusão.</param>
    /// <param name="data">Dia (fuso da escola) a que o registro auditado se refere, pareado com <paramref name="turmaId"/>.</param>
    /// <summary>Mesma coisa, mas pra ações do próprio sistema (sem usuário logado), ex.: baixa automática de um pagamento Pix.</summary>
    void RegistrarSistema(string entidadeTipo, Guid entidadeId, AcaoAuditoria acao, string? detalhe = null);

    void Registrar(string entidadeTipo, Guid entidadeId, AcaoAuditoria acao, Guid usuarioId, string? detalhe = null, Guid? turmaId = null, DateOnly? data = null);
}
