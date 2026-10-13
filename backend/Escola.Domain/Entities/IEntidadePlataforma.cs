namespace Escola.Domain.Entities;

/// <summary>Marca uma entidade que é do <b>produto</b>, não de um cliente (ex.: cupom de desconto da assinatura). Fica fora do isolamento por
/// cliente: sem coluna <c>ClienteId</c>, sem filtro global, e a exclusão de dados de um clube não mexe nela. Use com parcimônia — só para
/// dado da equipe do produto, nunca para dado de escola, aluno ou família.</summary>
public interface IEntidadePlataforma;
