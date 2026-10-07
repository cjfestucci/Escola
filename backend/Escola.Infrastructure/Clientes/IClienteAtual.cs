namespace Escola.Infrastructure.Clientes;

/// <summary>Cliente (tenant) da requisição atual. Todos os clientes compartilham o mesmo site e a mesma tela de login (desde
/// 2026-10-07): o cliente vem do <b>token</b> de quem está logado (claim <c>clienteId</c>), nunca de algo que o usuário digite.
/// Fluxos sem login (esqueci a senha, link do e-mail, webhook do banco) e tarefas de fundo definem o cliente explicitamente,
/// a partir do próprio dado que estão tratando.</summary>
public interface IClienteAtual
{
    /// <summary>Cliente definido, ou <see cref="Guid.Empty"/> se nenhum foi definido ainda — nesse caso o filtro global não devolve
    /// dado de ninguém e uma gravação falha na chave estrangeira (falha fechada, nunca "vê tudo").</summary>
    Guid Id { get; }

    bool Definido { get; }
}

/// <summary>Um por escopo de DI (requisição, ou escopo criado à mão numa tarefa de fundo).</summary>
public sealed class ClienteAtual : IClienteAtual
{
    private Guid? _id;

    public ClienteAtual()
    {
    }

    /// <summary>Já nasce definido (testes e ferramentas).</summary>
    public ClienteAtual(Guid id) => _id = id;

    public Guid Id => _id ?? Guid.Empty;

    public bool Definido => _id.HasValue;

    /// <summary>Define o cliente do escopo. Trocar de cliente no meio de um escopo é recusado: um mesmo DbContext atendendo dois
    /// clientes misturaria dados — pra outro cliente, crie outro escopo.</summary>
    public void Definir(Guid id)
    {
        if (id == Guid.Empty) throw new ArgumentException("Cliente inválido.", nameof(id));
        if (_id is { } atual && atual != id)
            throw new InvalidOperationException("O cliente deste escopo já foi definido; crie um escopo novo para outro cliente.");
        _id = id;
    }
}

public static class ClientePadrao
{
    /// <summary>Cliente que recebeu todos os dados já existentes quando o banco passou a ser multi-cliente (migration
    /// `AddClientes`) e valor padrão de <c>Cliente:Id</c> (o "cliente inicial" criado na primeira subida).</summary>
    public static readonly Guid Id = Guid.Parse("00000001-0000-0000-0000-000000000001");
}
