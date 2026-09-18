import { environment } from '../../environments/environment';
import { CategoriaRegistro, Humor, RegistroRotina, Refeicao, StatusAlimentacao } from '../models/registro-rotina.model';

const ICONES: Record<CategoriaRegistro, string> = {
  Alimentacao: '🍽️',
  Sono: '😴',
  Higiene: '🧷',
  Humor: '🙂',
  Momento: '📷'
};

const REFEICOES: Record<Refeicao, string> = { Cafe: 'Café', Almoco: 'Almoço', Lanche: 'Lanche' };
const STATUS_ALIMENTACAO: Record<StatusAlimentacao, string> = {
  ComeuTudo: 'comeu tudo',
  Parcial: 'comeu parte',
  Recusou: 'recusou'
};
const HUMORES: Record<Humor, string> = {
  Feliz: 'Feliz 😊',
  Agitado: 'Agitado 🙃',
  Sonolento: 'Sonolento 😪',
  Choroso: 'Choroso 😢'
};

export function iconeCategoria(categoria: CategoriaRegistro): string {
  return ICONES[categoria];
}

export function rotuloCategoria(registro: RegistroRotina): string {
  switch (registro.categoria) {
    case 'Alimentacao':
      return `${REFEICOES[registro.refeicao ?? 'Lanche']} — ${STATUS_ALIMENTACAO[registro.statusAlimentacao ?? 'Parcial']}`;
    case 'Sono':
      return registro.horaFim ? `Dormiu ${registro.horaInicio}–${registro.horaFim}` : `Dormiu às ${registro.horaInicio}`;
    case 'Higiene':
      return registro.tipoHigiene === 'TrocaFralda' ? 'Troca de fralda' : 'Foi ao banheiro';
    case 'Humor':
      return HUMORES[registro.humor ?? 'Feliz'];
    case 'Momento':
      return 'Momento registrado';
  }
}

export function horaRegistro(registradoEm: string): string {
  return new Date(registradoEm).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

/** As fotos vêm do backend como caminho relativo (ex.: "/uploads/xyz.jpg"); aqui vira URL completa. */
export function resolverFotoUrl(fotoUrl: string): string {
  return `${environment.fileOrigin}${fotoUrl}`;
}

export function resolverFotoUrls(fotoUrls: string[]): string[] {
  return fotoUrls.map(resolverFotoUrl);
}
