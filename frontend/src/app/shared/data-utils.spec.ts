import {
  definirFusoEscola,
  formatarDataAbsoluta,
  formatarDataHoraAbsoluta,
  formatarHora,
  hojeIso,
  horaAtualEscola,
  idadeFormatada,
  rotuloData,
  somarDias
} from './data-utils';

/**
 * "Hoje" é sempre o dia no fuso da escola — nunca o UTC nem o fuso do navegador. Antes dessa regra o dia
 * virava às 21h no Brasil (rotina lançada à noite caía no dia seguinte).
 */
describe('data-utils (fuso da escola)', () => {
  beforeEach(() => {
    jasmine.clock().install();
    definirFusoEscola('America/Sao_Paulo');
  });

  afterEach(() => {
    jasmine.clock().uninstall();
    definirFusoEscola('America/Sao_Paulo');
  });

  it('à noite em São Paulo ainda é o mesmo dia, mesmo já sendo outro dia em UTC', () => {
    jasmine.clock().mockDate(new Date('2026-11-01T01:30:00Z')); // 22h30 do dia 31/10 em São Paulo
    expect(hojeIso()).toBe('2026-10-31');
    expect(horaAtualEscola()).toBe('22:30');
  });

  it('respeita o fuso configurado na escola', () => {
    jasmine.clock().mockDate(new Date('2026-11-01T01:30:00Z'));
    definirFusoEscola('Asia/Tokyo'); // UTC+9 → 10h30 do dia 1º
    expect(hojeIso()).toBe('2026-11-01');
    expect(horaAtualEscola()).toBe('10:30');
  });

  it('somarDias atravessa mês e ano sem depender do fuso do navegador', () => {
    expect(somarDias('2026-12-31', 1)).toBe('2027-01-01');
    expect(somarDias('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('rotuloData diz Hoje e Ontem pelo dia da escola', () => {
    jasmine.clock().mockDate(new Date('2026-11-01T01:30:00Z')); // hoje = 2026-10-31
    expect(rotuloData('2026-10-31')).toBe('Hoje');
    expect(rotuloData('2026-10-30')).toBe('Ontem');
  });

  it('formata datas em português, sem o dia anterior que o fuso do navegador causaria', () => {
    expect(formatarDataAbsoluta('2026-10-05')).toBe('05/10/2026');
  });

  it('mostra timestamps UTC no fuso da escola', () => {
    expect(formatarHora('2026-10-20T13:05:00Z')).toBe('10:05');
    expect(formatarDataHoraAbsoluta('2026-11-01T01:30:00Z')).toBe('31/10/2026, 22:30');
  });

  it('idade em meses até 2 anos e em anos depois', () => {
    jasmine.clock().mockDate(new Date('2026-10-20T15:00:00Z'));
    expect(idadeFormatada('2025-10-20')).toBe('12 meses');
    expect(idadeFormatada('2025-10-21')).toBe('11 meses'); // ainda não fez o mês
    expect(idadeFormatada('2020-01-10')).toBe('6 anos');
  });
});
