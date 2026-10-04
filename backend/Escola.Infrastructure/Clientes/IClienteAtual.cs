namespace Escola.Infrastructure.Clientes;

/// <summary>Cliente (tenant) ao qual esta instalação do sistema pertence. Como cada cliente tem o próprio site e login,
/// isso vem da configuração do deploy (<c>Cliente:Id</c>) — nunca do usuário nem da requisição.</summary>
public interface IClienteAtual
{
    Guid Id { get; }
}

public sealed class ClienteAtual(Guid id) : IClienteAtual
{
    public Guid Id { get; } = id;
}

public static class ClientePadrao
{
    /// <summary>Cliente que recebeu todos os dados já existentes quando o banco passou a ser multi-cliente (migration
    /// `AddClientes`) e valor padrão de <c>Cliente:Id</c> — uma instalação antiga continua funcionando sem configurar nada.</summary>
    public static readonly Guid Id = Guid.Parse("00000001-0000-0000-0000-000000000001");
}
