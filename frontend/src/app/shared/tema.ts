/**
 * Tema do app: a escola escolhe uma cor principal e o resto da paleta (tom escuro, tons claros, barra
 * lateral) é derivado dela aqui, sobrescrevendo as variáveis CSS de `styles.scss` na raiz do documento.
 * Sem cor escolhida (null) as variáveis inline são removidas e valem os padrões do CSS.
 */

export const COR_PADRAO = '#6C5DD3';

/** Mesmo mínimo que o backend exige (WCAG AA): a cor é fundo de botão/menu com texto branco por cima. */
export const CONTRASTE_MINIMO = 4.5;

export const CORES_SUGERIDAS: { valor: string; rotulo: string }[] = [
  { valor: COR_PADRAO, rotulo: 'Roxo (padrão)' },
  { valor: '#2563EB', rotulo: 'Azul' },
  { valor: '#0F766E', rotulo: 'Turquesa' },
  { valor: '#15803D', rotulo: 'Verde' },
  { valor: '#C2410C', rotulo: 'Laranja' },
  { valor: '#DC2626', rotulo: 'Vermelho' },
  { valor: '#BE185D', rotulo: 'Rosa' },
  { valor: '#374151', rotulo: 'Grafite' }
];

const VARIAVEIS = [
  '--cor-primaria',
  '--cor-primaria-escura',
  '--cor-primaria-clara',
  '--cor-primaria-suave',
  '--cor-sidebar-fundo',
  '--cor-stat-roxo-fundo',
  '--cor-stat-roxo-texto'
];

type Rgb = [number, number, number];

export function corValida(cor: string | null | undefined): cor is string {
  return !!cor && /^#[0-9a-fA-F]{6}$/.test(cor);
}

/** Aceita "6c5dd3", "#6c5dd3" ou "#6C5DD3" e devolve "#6C5DD3" (ou o texto como veio, se não for uma cor). */
export function normalizarCor(texto: string): string {
  const limpo = texto.trim().replace(/^#?/, '#').toUpperCase();
  return corValida(limpo) ? limpo : texto.trim();
}

function paraRgb(cor: string): Rgb {
  return [1, 3, 5].map((i) => parseInt(cor.slice(i, i + 2), 16)) as Rgb;
}

function paraHex([r, g, b]: Rgb): string {
  return '#' + [r, g, b].map((c) => Math.round(c).toString(16).padStart(2, '0')).join('');
}

function misturar(cor: string, com: Rgb, quantidade: number): string {
  const base = paraRgb(cor);
  return paraHex(base.map((c, i) => c + (com[i] - c) * quantidade) as Rgb);
}

export function contrasteComBranco(cor: string): number {
  const [r, g, b] = paraRgb(cor).map((c) => {
    const v = c / 255;
    return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4);
  });
  return 1.05 / (0.2126 * r + 0.7152 * g + 0.0722 * b + 0.05);
}

/** Texto de por que a cor não serve, ou null se serve. */
export function problemaDaCor(cor: string): string | null {
  if (!corValida(cor)) return 'Cor inválida. Use o formato #RRGGBB, por exemplo #6C5DD3.';
  if (contrasteComBranco(cor) < CONTRASTE_MINIMO) {
    return 'Essa cor é clara demais: o texto branco dos botões e do menu ficaria difícil de ler. Escolha uma cor mais escura.';
  }
  return null;
}

const BRANCO: Rgb = [255, 255, 255];
const PRETO: Rgb = [0, 0, 0];

export function aplicarTema(cor: string | null | undefined): void {
  const estilo = document.documentElement.style;
  if (!corValida(cor) || problemaDaCor(cor)) {
    for (const nome of VARIAVEIS) estilo.removeProperty(nome);
    return;
  }

  const clara = misturar(cor, BRANCO, 0.9);
  estilo.setProperty('--cor-primaria', cor);
  estilo.setProperty('--cor-primaria-escura', misturar(cor, PRETO, 0.2));
  estilo.setProperty('--cor-primaria-clara', clara);
  estilo.setProperty('--cor-primaria-suave', misturar(cor, BRANCO, 0.72));
  estilo.setProperty('--cor-sidebar-fundo', misturar(cor, PRETO, 0.12));
  estilo.setProperty('--cor-stat-roxo-fundo', clara);
  estilo.setProperty('--cor-stat-roxo-texto', cor);
}
