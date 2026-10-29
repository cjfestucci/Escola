// Grava o Reel "Chamada em menos de 1 minuto" no app local (clube Demonstração FC), numa tela de celular 9:16, e monta o MP4.
const { chromium } = require('playwright-core');
const { execSync, spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const APP = 'http://localhost:4200';
const EMAIL = 'treinador.demo@clube-teste.test';
const SENHA = fs.readFileSync(path.join(__dirname, '..', 'senha-admin-baas.txt'), 'utf8').trim();
const FFMPEG = process.env.LOCALAPPDATA + '\\Microsoft\\WinGet\\Links\\ffmpeg.exe';
const SAIDA = process.argv[2] || path.join(__dirname, 'reel-chamada.mp4');
const PASTA_FRAMES = path.join(__dirname, 'frames');

const LARGURA = 405, ALTURA = 720, ESCALA = 1080 / 405; // 1080x1920 reais

const espera = (ms) => new Promise((r) => setTimeout(r, ms));

(async () => {
  // Estado limpo: apaga a chamada de hoje do clube Demonstração (a gravação salva uma chamada nova).
  execSync(`docker exec escola-postgres psql -U postgres -d escola_dev -v ON_ERROR_STOP=1 -c "DELETE FROM \\"RegistrosPresenca\\" r USING \\"Clientes\\" c WHERE c.\\"Id\\"=r.\\"ClienteId\\" AND c.\\"Nome\\"='Demonstração FC' AND r.\\"Data\\" >= CURRENT_DATE - 1"`, { stdio: 'ignore' });

  const navegador = await chromium.launch({ channel: 'chrome', headless: true });

  // Login fora da gravação.
  const ctxLogin = await navegador.newContext({ viewport: { width: LARGURA, height: ALTURA } });
  const pLogin = await ctxLogin.newPage();
  await pLogin.goto(APP + '/entrar');
  await pLogin.fill('input[type=email]', EMAIL);
  await pLogin.fill('input[type=password]', SENHA);
  await pLogin.click('button[type=submit]');
  await pLogin.waitForURL((u) => !u.pathname.includes('entrar'), { timeout: 20000 });
  const estado = await ctxLogin.storageState();
  await ctxLogin.close();

  const ctx = await navegador.newContext({ viewport: { width: LARGURA, height: ALTURA }, deviceScaleFactor: ESCALA, storageState: estado, isMobile: true, hasTouch: true, locale: 'pt-BR' });
  const page = await ctx.newPage();

  // Captura de quadros (CDP screencast) com a hora de cada um.
  fs.rmSync(PASTA_FRAMES, { recursive: true, force: true });
  fs.mkdirSync(PASTA_FRAMES, { recursive: true });
  const cdp = await ctx.newCDPSession(page);
  const quadros = [];
  cdp.on('Page.screencastFrame', async (f) => {
    const arq = path.join(PASTA_FRAMES, `f${String(quadros.length).padStart(5, '0')}.jpg`);
    fs.writeFileSync(arq, Buffer.from(f.data, 'base64'));
    quadros.push({ arq, t: f.metadata.timestamp });
    cdp.send('Page.screencastFrameAck', { sessionId: f.sessionId }).catch(() => {});
  });

  await cdp.send('Page.startScreencast', { format: 'jpeg', quality: 92, maxWidth: 1080, maxHeight: 1920, everyNthFrame: 1 });
  await page.goto(APP + '/chamada');
  await page.waitForSelector('text=Todos presentes');

  // Visual de gravação: sem faixa de teste, rolagem suave, e as camadas de legenda/toque.
  await page.addStyleTag({ content: `
    app-aviso-assinatura, app-aviso-conexao { display: none !important; }
    html { scroll-behavior: smooth; }
    #rl-cartao { position: fixed; inset: 0; z-index: 99999; background: #0E2A3A; color: #fff; display: flex; flex-direction: column;
      align-items: center; justify-content: center; text-align: center; padding: 32px; font-family: Inter, system-ui, sans-serif;
      opacity: 0; transition: opacity .35s ease; pointer-events: none; }
    #rl-cartao.on { opacity: 1; }
    #rl-cartao .marca { font-size: 15px; font-weight: 700; letter-spacing: .04em; opacity: .85; margin-bottom: 28px; }
    #rl-cartao h1 { font-size: 34px; line-height: 1.12; margin: 0 0 14px; font-weight: 800; }
    #rl-cartao h1 em { font-style: normal; color: #4ADE80; }
    #rl-cartao p { font-size: 17px; opacity: .9; margin: 0; }
    #rl-cartao .botao { margin-top: 26px; background: #16A34A; color: #fff; font-weight: 800; font-size: 18px; padding: 13px 26px; border-radius: 40px; }
    #rl-legenda { position: fixed; left: 14px; right: 14px; top: 70px; z-index: 99998; background: rgba(14,42,58,.94); color: #fff; font: 700 15px/1.3 Inter, system-ui, sans-serif;
      padding: 10px 14px; border-radius: 14px; text-align: center; opacity: 0; transform: translateY(-6px); transition: all .3s ease; pointer-events: none; }
    #rl-legenda.on { opacity: 1; transform: none; }
    #rl-toque { position: fixed; z-index: 99997; width: 46px; height: 46px; margin: -23px 0 0 -23px; border-radius: 50%; background: rgba(22,163,74,.35);
      border: 3px solid #16A34A; pointer-events: none; opacity: 0; transform: scale(.4); }
    #rl-toque.on { animation: rl-toque .5s ease-out; }
    @keyframes rl-toque { 0% { opacity: 0; transform: scale(.4); } 25% { opacity: 1; } 100% { opacity: 0; transform: scale(1.35); } }
  ` });
  await page.evaluate(() => {
    const c = document.createElement('div'); c.id = 'rl-cartao'; document.body.appendChild(c);
    const l = document.createElement('div'); l.id = 'rl-legenda'; document.body.appendChild(l);
    const t = document.createElement('div'); t.id = 'rl-toque'; document.body.appendChild(t);
  });

  const cartao = (html) => page.evaluate((h) => { const c = document.getElementById('rl-cartao'); if (h) { c.innerHTML = h; c.classList.add('on'); } else c.classList.remove('on'); }, html);
  const legenda = (txt) => page.evaluate((s) => { const l = document.getElementById('rl-legenda'); if (s) { l.textContent = s; l.classList.add('on'); } else l.classList.remove('on'); }, txt);
  const rolarPara = async (loc) => { await loc.evaluate((el) => el.scrollIntoView({ behavior: 'smooth', block: 'center' })); await espera(900); };
  const tocar = async (loc) => {
    const caixa = await loc.boundingBox();
    const x = caixa.x + caixa.width / 2, y = caixa.y + caixa.height / 2;
    await page.evaluate(([px, py]) => { const t = document.getElementById('rl-toque'); t.style.left = px + 'px'; t.style.top = py + 'px'; t.classList.remove('on'); void t.offsetWidth; t.classList.add('on'); }, [x, y]);
    await espera(220);
    await loc.click();
    await espera(500);
  };
  const botaoDe = (nome, rotulo) => page.locator(`xpath=//*[normalize-space(text())="${nome}"]/ancestor::*[.//button[normalize-space()="${rotulo}"]][1]`).getByRole('button', { name: rotulo, exact: true });

  // --- Roteiro ---
  await cartao('<div class="marca">⚽ GESTOR TÁTICO</div><h1>Chamada do treino em<br><em>menos de 1 minuto</em></h1><p>No celular, do campo.</p>');
  await espera(2600);
  await cartao(null);
  await espera(900);

  await legenda('1. Toque em “Todos presentes”');
  await espera(900);
  await tocar(page.getByRole('button', { name: '✓ Todos presentes' }));
  await espera(1100);

  await legenda('2. Ajuste só as exceções');
  await rolarPara(page.getByText('Pedro Henrique Souza', { exact: true }));
  await tocar(botaoDe('Pedro Henrique Souza', 'Falta'));
  await espera(500);
  await rolarPara(page.getByText('Enzo Martins', { exact: true }));
  await tocar(botaoDe('Enzo Martins', 'Justif.'));
  await espera(900);

  await legenda('3. Salve');
  await page.evaluate(() => window.scrollTo({ top: 0, behavior: 'smooth' }));
  await espera(900);
  await tocar(page.getByRole('button', { name: 'Salvar chamada' }));
  await espera(1800);

  await legenda('A frequência sai pronta, e quem falta seguido ganha alerta');
  await rolarPara(page.getByText('Pedro Henrique Souza', { exact: true }));
  await espera(2600);
  await legenda(null);
  await espera(300);

  await cartao('<div class="marca">⚽ GESTOR TÁTICO</div><h1>Teste grátis por<br><em>7 dias</em></h1><p>Gestão para escolinhas e clubes de base</p><div class="botao">Link na bio</div>');
  await espera(3200);

  await cdp.send('Page.stopScreencast').catch(() => {});
  await navegador.close();

  // --- Monta o MP4 a partir dos quadros e das suas horas ---
  if (quadros.length < 2) throw new Error('poucos quadros capturados: ' + quadros.length);
  const lista = [];
  for (let i = 0; i < quadros.length; i++) {
    const dur = i < quadros.length - 1 ? Math.max(0.001, quadros[i + 1].t - quadros[i].t) : 1.2;
    lista.push(`file '${quadros[i].arq.replace(/\\/g, '/')}'`, `duration ${dur.toFixed(4)}`);
  }
  lista.push(`file '${quadros[quadros.length - 1].arq.replace(/\\/g, '/')}'`);
  const arqLista = path.join(__dirname, 'frames.txt');
  fs.writeFileSync(arqLista, lista.join('\n'));
  const r = spawnSync(FFMPEG, ['-y', '-f', 'concat', '-safe', '0', '-i', arqLista, '-vf', 'fps=30,scale=1080:1920,format=yuv420p', '-c:v', 'libx264', '-preset', 'medium', '-crf', '20', '-movflags', '+faststart', '-an', SAIDA], { encoding: 'utf8' });
  if (r.status !== 0) throw new Error('ffmpeg falhou: ' + r.stderr.slice(-600));
  const dur = quadros[quadros.length - 1].t - quadros[0].t + 1.2;
  console.log(`MP4 gerado: ${SAIDA} (${quadros.length} quadros, ~${dur.toFixed(1)} s)`);
})().catch((e) => { console.error('ERRO:', e.message); process.exit(1); });
