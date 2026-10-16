/** Máscaras de digitação dos documentos e contatos brasileiros. O backend sempre guarda/valida sem a pontuação, então a máscara é só
 * apresentação: o que vai pro modelo é o texto formatado, e quem precisa do valor limpo usa `soDigitos` (telefone, CPF, CEP) ou
 * `soDocumento` (CPF/CNPJ — o CNPJ alfanumérico da Receita tem letras nas 12 primeiras posições). */
export type TipoMascara = 'telefone' | 'cpf' | 'cnpj' | 'cpfCnpj' | 'cep';

export function soDigitos(texto: string | null | undefined): string {
  return (texto ?? '').replace(/\D/g, '');
}

/** CPF/CNPJ sem pontuação, letras em maiúsculas: "12.abc.345/01de-35" → "12ABC34501DE35". */
export function soDocumento(texto: string | null | undefined): string {
  return (texto ?? '').toUpperCase().replace(/[^0-9A-Z]/g, '');
}

/** Encaixa os dígitos no padrão (`#` = um dígito), parando onde os dígitos acabam — assim o separador só aparece depois que o
 * próximo dígito é digitado e o apagar não "trava" num traço ou parêntese. */
function aplicar(digitos: string, padrao: string): string {
  let saida = '';
  let i = 0;
  for (const c of padrao) {
    if (i >= digitos.length) break;
    if (c === '#') saida += digitos[i++];
    else saida += c;
  }
  return saida;
}

export function formatarTelefone(texto: string | null | undefined): string {
  let d = soDigitos(texto);
  // Colou com o código do país (+55 11 99999-8888): fica só DDD + número.
  if (d.length > 11 && d.startsWith('55')) d = d.slice(2);
  d = d.slice(0, 11);
  if (d.length <= 2) return d.length ? `(${d}` : '';
  // Fixo (10 dígitos) × celular (11): o traço muda de lugar quando o 11º dígito chega.
  return aplicar(d, d.length <= 10 ? '(##) ####-####' : '(##) #####-####');
}

export function formatarCpf(texto: string | null | undefined): string {
  return aplicar(soDigitos(texto).slice(0, 11), '###.###.###-##');
}

/** CNPJ, inclusive o alfanumérico: letras ou números nas 12 primeiras posições; as 2 últimas (dígitos verificadores) só números. */
export function formatarCnpj(texto: string | null | undefined): string {
  let valor = '';
  for (const c of soDocumento(texto)) {
    if (valor.length >= 14) break;
    if (valor.length >= 12 && !/\d/.test(c)) continue;
    valor += c;
  }
  return aplicar(valor, '##.###.###/####-##');
}

/** CPF até 11 números; vira CNPJ a partir do 12º caractere ou assim que aparece uma letra (CNPJ alfanumérico). */
export function formatarCpfCnpj(texto: string | null | undefined): string {
  const valor = soDocumento(texto);
  return valor.length <= 11 && /^\d*$/.test(valor) ? formatarCpf(valor) : formatarCnpj(valor);
}

export function formatarCep(texto: string | null | undefined): string {
  return aplicar(soDigitos(texto).slice(0, 8), '#####-###');
}

export function formatarPorMascara(tipo: TipoMascara, texto: string | null | undefined): string {
  switch (tipo) {
    case 'telefone': return formatarTelefone(texto);
    case 'cpf': return formatarCpf(texto);
    case 'cnpj': return formatarCnpj(texto);
    case 'cpfCnpj': return formatarCpfCnpj(texto);
    case 'cep': return formatarCep(texto);
  }
}

/** `maxlength` do campo: o tamanho formatado (o telefone tem folga pra colar com "+55 ").  */
export const TAMANHO_MAXIMO: Record<TipoMascara, number> = { telefone: 20, cpf: 14, cnpj: 18, cpfCnpj: 18, cep: 9 };

/** Link que abre a conversa no WhatsApp (app no celular, WhatsApp Web no computador). Número brasileiro com DDD → 55 + DDD + número;
 * nulo se o número não tiver DDD. Com `texto`, a conversa abre com essa mensagem já escrita (a pessoa completa e envia). */
export function linkWhatsApp(telefone: string | null | undefined, texto?: string | null): string | null {
  const d = soDigitos(telefone);
  const numero = d.length === 10 || d.length === 11 ? `55${d}` : (d.length === 12 || d.length === 13) && d.startsWith('55') ? d : null;
  if (!numero) return null;
  return texto ? `https://wa.me/${numero}?text=${encodeURIComponent(texto)}` : `https://wa.me/${numero}`;
}
