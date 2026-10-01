import QRCode from 'qrcode';

/** Gera o QR Code do código Pix inteiramente no navegador — o código nunca sai pra nenhum serviço externo. */
export function gerarQrCodePix(codigoCopiaECola: string): Promise<string> {
  return QRCode.toDataURL(codigoCopiaECola, { width: 220, margin: 1 });
}
