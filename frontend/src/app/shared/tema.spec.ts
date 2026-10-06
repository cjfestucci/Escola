import { COR_PADRAO, CORES_SUGERIDAS, aplicarTema, contrasteComBranco, corValida, normalizarCor, problemaDaCor } from './tema';

describe('tema', () => {
  afterEach(() => aplicarTema(null));

  it('todas as cores sugeridas passam no contraste mínimo (texto branco legível)', () => {
    for (const { valor, rotulo } of CORES_SUGERIDAS) {
      expect(problemaDaCor(valor)).withContext(rotulo).toBeNull();
    }
    expect(problemaDaCor(COR_PADRAO)).toBeNull();
  });

  it('recusa cor clara demais e formato inválido, com mensagem em português', () => {
    expect(problemaDaCor('#FFEB3B')).toContain('clara demais');
    expect(problemaDaCor('#CCCCCC')).toContain('clara demais');
    expect(problemaDaCor('azul')).toContain('Cor inválida');
  });

  it('contraste de preto e de branco contra branco', () => {
    expect(contrasteComBranco('#000000')).toBeCloseTo(21, 0);
    expect(contrasteComBranco('#FFFFFF')).toBeCloseTo(1.05 / 1.05, 1);
  });

  it('normaliza o que o usuário digita', () => {
    expect(normalizarCor('6c5dd3')).toBe('#6C5DD3');
    expect(normalizarCor('  #2563eb ')).toBe('#2563EB');
    expect(normalizarCor('xyz')).toBe('xyz');
    expect(corValida('#6C5DD3')).toBeTrue();
    expect(corValida('#6C5DD')).toBeFalse();
    expect(corValida(null)).toBeFalse();
  });

  it('aplica a paleta derivada nas variáveis CSS e remove ao voltar pro padrão', () => {
    aplicarTema('#2563EB');
    const estilo = document.documentElement.style;
    expect(estilo.getPropertyValue('--cor-primaria')).toBe('#2563EB');
    expect(estilo.getPropertyValue('--cor-primaria-clara')).not.toBe('');
    expect(estilo.getPropertyValue('--cor-sidebar-fundo')).not.toBe('');

    aplicarTema(null);
    expect(estilo.getPropertyValue('--cor-primaria')).toBe('');
  });

  it('uma cor inaceitável nunca é aplicada (cai no padrão do produto)', () => {
    aplicarTema('#FFEB3B');
    expect(document.documentElement.style.getPropertyValue('--cor-primaria')).toBe('');
  });
});
