export interface LogAuditoria {
  id: string;
  acao: 'Criado' | 'Editado' | 'Excluido';
  usuarioNome: string;
  detalhe: string | null;
  registradoEm: string;
}
