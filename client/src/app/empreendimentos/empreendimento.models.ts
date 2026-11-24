export type PorteEmpreendimento = number;
export type StatusLicenciamento = number;

export interface CriarEmpreendimentoRequest {
  readonly razaoSocial: string;
  readonly nomeFantasia: string;
  readonly cnpj: string;
  readonly municipio: string;
  readonly uf: string;
  readonly porte: PorteEmpreendimento;
  readonly statusLicenciamento: StatusLicenciamento;
}

export interface AtualizarEmpreendimentoRequest {
  readonly razaoSocial: string;
  readonly nomeFantasia: string;
  readonly cnpj: string;
  readonly municipio: string;
  readonly uf: string;
  readonly porte: PorteEmpreendimento;
  readonly statusLicenciamento: StatusLicenciamento;
}

export interface EmpreendimentoDto {
  readonly id: string;
  readonly razaoSocial: string;
  readonly nomeFantasia: string;
  readonly cnpj: string;
  readonly municipio: string;
  readonly uf: string;
  readonly porte: string;
  readonly statusLicenciamento: string;
  readonly createdAtUtc?: string;
  readonly updatedAtUtc?: string;
  readonly quantidadeLicencas?: number;
}