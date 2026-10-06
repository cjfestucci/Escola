using Escola.Domain.Enums;

namespace Escola.Api.Servicos;

/// <summary>Termo de matrícula e de tratamento de dados pessoais (LGPD) que o responsável aceita no primeiro acesso ao portal.
/// <para><b>Mudou o texto? Troque a <see cref="Versao"/>.</b> Cada aceite guarda o texto exato que foi mostrado, e o portal volta a pedir o
/// aceite a todos os responsáveis quando a versão muda (as matrículas já confirmadas continuam confirmadas).</para>
/// <para>É um texto-base: deve ser revisado por quem cuida da parte jurídica de cada escola antes de ir pra produção.</para></summary>
public static class TermoMatricula
{
    public const string Versao = "2026-10-06";

    public const string Titulo = "Termo de matrícula e de tratamento de dados pessoais";

    public static IReadOnlyList<string> Paragrafos(string nomeEscola, string nomeResponsavel, IReadOnlyList<string> nomesAlunos, SegmentoCliente segmento)
    {
        var pessoa = segmento == SegmentoCliente.Clube ? "atleta" : "aluno(a)";
        var servico = segmento == SegmentoCliente.Clube ? "as atividades esportivas" : "o serviço educacional";
        var alunos = nomesAlunos.Count switch
        {
            0 => string.Empty,
            1 => nomesAlunos[0],
            _ => string.Join(", ", nomesAlunos.Take(nomesAlunos.Count - 1)) + " e " + nomesAlunos[^1]
        };

        return
        [
            $"Eu, {nomeResponsavel}, declaro ser responsável por {alunos} e confirmo a matrícula junto a {nomeEscola}. " +
            "Estou ciente de que a matrícula só é efetivada após este aceite.",

            $"Para prestar {servico}, {nomeEscola} trata os seguintes dados pessoais: dados de cadastro do(a) {pessoa} e dos responsáveis " +
            "(nome, data de nascimento, e-mail, telefone, foto); dados de saúde informados na ficha de saúde (alergias, medicamentos, " +
            "condições de saúde, atestados e documentos anexados); registros do dia a dia (frequência, rotina, atividades e fotos); e dados " +
            "financeiros (mensalidades e pagamentos).",

            "Esses dados são usados para: realizar a matrícula e as atividades; cuidar da saúde e da segurança do(a) " + pessoa + "; " +
            "comunicar-se com a família; cobrar as mensalidades; e cumprir obrigações legais.",

            "Os dados de saúde são dados sensíveis: só a equipe da instituição tem acesso a eles, e apenas para proteger a saúde e a vida " +
            $"do(a) {pessoa}.",

            "Os dados não são vendidos. São compartilhados apenas quando necessário: com o banco, para pagamentos por Pix; com os " +
            "fornecedores de tecnologia que hospedam o sistema; ou com autoridades, quando a lei exigir.",

            "Os dados são guardados enquanto durar a matrícula e, depois dela, pelo prazo exigido por lei (por exemplo, registros escolares " +
            "e fiscais). O que não precisar mais ser guardado é eliminado.",

            "Como responsável, posso pedir a qualquer momento: acesso e cópia dos dados, correção do que estiver errado, eliminação do que " +
            "não precisa ser guardado por lei e informações sobre o uso dos dados. Os pedidos são feitos à secretaria/coordenação da " +
            "instituição.",
        ];
    }

    /// <summary>O texto como ficou guardado no aceite (título + parágrafos), pra servir de prova do que foi mostrado.</summary>
    public static string TextoCompleto(IReadOnlyList<string> paragrafos) => $"{Titulo} (versão {Versao})\n\n" + string.Join("\n\n", paragrafos);
}
