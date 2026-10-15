import { formatarCep, formatarCnpj, formatarCpf, formatarCpfCnpj, formatarTelefone, soDigitos } from './mascara';

describe('máscaras', () => {
  it('telefone: fixo, celular, digitação parcial e colado com +55', () => {
    expect(formatarTelefone('')).toBe('');
    expect(formatarTelefone('1')).toBe('(1');
    expect(formatarTelefone('11')).toBe('(11');
    expect(formatarTelefone('119')).toBe('(11) 9');
    expect(formatarTelefone('1133334444')).toBe('(11) 3333-4444');
    expect(formatarTelefone('11999998888')).toBe('(11) 99999-8888');
    expect(formatarTelefone('+55 11 99999-8888')).toBe('(11) 99999-8888');
    expect(formatarTelefone('(11)999999999')).toBe('(11) 99999-9999');
    expect(formatarTelefone('119999988887777')).toBe('(11) 99999-8888');
  });

  it('CPF, CNPJ e CEP', () => {
    expect(formatarCpf('52998224725')).toBe('529.982.247-25');
    expect(formatarCpf('5299')).toBe('529.9');
    expect(formatarCpf('529')).toBe('529');
    expect(formatarCnpj('11222333000181')).toBe('11.222.333/0001-81');
    expect(formatarCep('14055470')).toBe('14055-470');
    expect(formatarCep('140554')).toBe('14055-4');
  });

  it('CPF ou CNPJ: vira CNPJ no 12º dígito', () => {
    expect(formatarCpfCnpj('52998224725')).toBe('529.982.247-25');
    expect(formatarCpfCnpj('112223330001')).toBe('11.222.333/0001');
    expect(formatarCpfCnpj('11.222.333/0001-81')).toBe('11.222.333/0001-81');
  });

  it('soDigitos', () => {
    expect(soDigitos('(11) 99999-8888')).toBe('11999998888');
    expect(soDigitos(null)).toBe('');
  });
});
