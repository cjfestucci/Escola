// Monta, no banco de DEV, o clube "Demonstração FC" com uma turma de 12 atletas fictícios e 4 treinos de histórico (pra gravar o Reel).
const { execSync } = require('child_process');
const fs = require('fs');
const API = 'http://localhost:5080';
const SENHA = fs.readFileSync(__dirname + '/../senha-admin-baas.txt', 'utf8').trim();
const HASH = 'zWXSlh4eno+uMBXcCBYaOw==.tQqOCZTe0iTndz+xMIL/rYSJ04aJ+1bIaAgQDuT6C4E=';
const EMAIL = 'treinador.demo@clube-teste.test';

function cnpj(base12) {
  const base = base12.split('').map(Number);
  const dv = (n) => { const p = n.length === 12 ? [5,4,3,2,9,8,7,6,5,4,3,2] : [6,5,4,3,2,9,8,7,6,5,4,3,2]; const r = n.reduce((a, v, i) => a + v * p[i], 0) % 11; return r < 2 ? 0 : 11 - r; };
  const d1 = dv(base), d2 = dv([...base, d1]);
  return [...base, d1, d2].join('');
}
async function chamar(metodo, caminho, corpo, token) {
  const r = await fetch(API + caminho, { method: metodo, headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: 'Bearer ' + token } : {}) }, body: corpo ? JSON.stringify(corpo) : undefined });
  const t = await r.text();
  if (!r.ok) throw new Error(`${metodo} ${caminho} -> ${r.status} ${t.slice(0, 300)}`);
  return t ? JSON.parse(t) : null;
}

(async () => {
  await chamar('POST', '/api/assinaturas', { nomeClube: 'Demonstração FC', cpfCnpj: cnpj('781234560001'), cidade: 'Ribeirao Preto', atletas: 25, nomeAdmin: 'Treinador Demo', email: EMAIL, celular: '00900000000', aceiteTermos: true, tokenCaptcha: 'XXXX.DUMMY.TOKEN.XXXX' });
  execSync(`docker exec escola-postgres psql -U postgres -d escola_dev -v ON_ERROR_STOP=1 -c "UPDATE \\"Usuarios\\" u SET \\"SenhaHash\\"='${HASH}' FROM \\"Clientes\\" c WHERE c.\\"Id\\"=u.\\"ClienteId\\" AND c.\\"Nome\\"='Demonstração FC' AND u.\\"Email\\"='${EMAIL}'"`, { stdio: 'inherit' });

  const { token } = await chamar('POST', '/api/auth/entrar', { email: EMAIL, senha: SENHA });
  const unidades = await chamar('GET', '/api/unidades', null, token);
  const turma = await chamar('POST', '/api/turmas', { nome: 'Sub-11', periodo: 0, horarioEntrada: '15:00:00', horarioSaida: '17:00:00', professorId: null, unidadeId: unidades[0].id, valorMensalidade: 150 }, token);

  const nomes = ['Pedro Henrique Souza', 'Lucas Almeida', 'Gabriel Costa', 'Mateus Ribeiro', 'João Vitor Lima', 'Enzo Martins',
                 'Davi Carvalho', 'Miguel Rocha', 'Arthur Gomes', 'Heitor Barbosa', 'Bernardo Dias', 'Samuel Teixeira'];
  const ids = [];
  for (let i = 0; i < nomes.length; i++) {
    const aluno = await chamar('POST', '/api/alunos', {
      nome: nomes[i], dataNascimento: `201${4 + (i % 2)}-0${1 + (i % 9)}-1${i % 9}`, turmaId: turma.id, fotoUrl: null,
      responsaveis: [{ id: null, nome: `Responsável ${nomes[i].split(' ')[0]}`, email: `resp.demo${i + 1}@pais.test`, telefone: null, responsavelFinanceiro: true }]
    }, token);
    ids.push((aluno.aluno && aluno.aluno.id) || aluno.id);
  }

  // Histórico: 4 treinos. Pedro (0) faltou nos dois últimos (a falta de hoje vira "3 seguidas"); Gabriel (2) faltou no dia 20.
  const dias = [['2026-10-15', {}], ['2026-10-20', { 2: 1 }], ['2026-10-22', { 0: 1, 5: 2 }], ['2026-10-27', { 0: 1 }]];
  for (const [data, excecoes] of dias) {
    await chamar('PUT', `/api/turmas/${turma.id}/presenca`, { data, itens: ids.map((id, i) => ({ alunoId: id, status: excecoes[i] ?? 0 })) }, token);
  }
  fs.writeFileSync(__dirname + '/demo.json', JSON.stringify({ email: EMAIL, turmaId: turma.id, atletas: ids.length }));
  console.log('Demonstração FC pronta:', ids.length, 'atletas, turma', turma.id);
})().catch((e) => { console.error(e.message); process.exit(1); });
