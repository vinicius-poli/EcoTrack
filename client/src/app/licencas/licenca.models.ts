export type TipoLicenca = number | string;
export type StatusLicenca = number | string;

export interface CriarLicencaRequest {
  readonly empreendimentoId: string;
  readonly numeroProcesso: string;
  readonly numeroLicenca: string;
  readonly orgaoEmissor: string;
  readonly tipo: number;
  readonly dataEmissao: string;
  readonly dataValidade: string;
  readonly diasAntecedenciaRenovacao: number;
  readonly status?: number;
  readonly numeroProtocoloRenovacao?: string | null;
  readonly dataProtocoloRenovacao?: string | null;
}

export interface AtualizarLicencaRequest {
  readonly empreendimentoId: string;
  readonly numeroProcesso: string;
  readonly numeroLicenca: string;
  readonly orgaoEmissor: string;
  readonly tipo: number;
  readonly dataEmissao: string;
  readonly dataValidade: string;
  readonly diasAntecedenciaRenovacao: number;
  readonly status: number;
  readonly numeroProtocoloRenovacao?: string | null;
  readonly dataProtocoloRenovacao?: string | null;
}

export interface LicencaDto {
  readonly id: string;
  readonly empreendimentoId: string;
  readonly numeroProcesso: string;
  readonly numeroLicenca: string;
  readonly orgaoEmissor: string;
  readonly tipo: TipoLicenca;
  readonly dataEmissao: string;
  readonly dataValidade: string;
  readonly diasAntecedenciaRenovacao: number;
  readonly dataLimiteProtocoloRenovacao: string;
  readonly status: StatusLicenca;
  readonly numeroProtocoloRenovacao?: string | null;
  readonly dataProtocoloRenovacao?: string | null;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}