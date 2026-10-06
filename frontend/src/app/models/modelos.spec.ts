import { formatarQuantidade, formatarTamanho } from '../shared/numero-utils';
import { resultadoDoJogo } from './competicao.model';
import { FALTAS_SEGUIDAS_PARA_ALERTA } from './presenca.model';

describe('resultadoDoJogo', () => {
  it('sai do placar e só existe em jogo realizado', () => {
    expect(resultadoDoJogo({ status: 'Realizado', golsPro: 3, golsContra: 1 })).toBe('V');
    expect(resultadoDoJogo({ status: 'Realizado', golsPro: 2, golsContra: 2 })).toBe('E');
    expect(resultadoDoJogo({ status: 'Realizado', golsPro: 0, golsContra: 1 })).toBe('D');
    expect(resultadoDoJogo({ status: 'Agendado', golsPro: null, golsContra: null })).toBeNull();
    expect(resultadoDoJogo({ status: 'Cancelado', golsPro: 1, golsContra: 0 })).toBeNull();
    expect(resultadoDoJogo({ status: 'Realizado', golsPro: null, golsContra: null })).toBeNull();
  });

  it('0 x 0 é empate (zero não é "sem placar")', () => {
    expect(resultadoDoJogo({ status: 'Realizado', golsPro: 0, golsContra: 0 })).toBe('E');
  });
});

describe('formatação numérica em pt-BR', () => {
  it('quantidade usa vírgula decimal e não deixa zeros sobrando', () => {
    expect(formatarQuantidade(1.5)).toBe('1,5');
    expect(formatarQuantidade(10)).toBe('10');
    expect(formatarQuantidade(0.125)).toBe('0,125');
  });

  it('tamanho de arquivo em B, KB e MB', () => {
    expect(formatarTamanho(512)).toBe('512 B');
    expect(formatarTamanho(2048)).toBe('2 KB');
    expect(formatarTamanho(1.5 * 1024 * 1024)).toBe('1,5 MB');
  });
});

describe('alerta de faltoso', () => {
  // O mesmo valor existe no backend (FrequenciaCalculo.FaltasSeguidasParaAlerta); os dois lados têm que andar juntos.
  it('são 3 faltas seguidas', () => expect(FALTAS_SEGUIDAS_PARA_ALERTA).toBe(3));
});
