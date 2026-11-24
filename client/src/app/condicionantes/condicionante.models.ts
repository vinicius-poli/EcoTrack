export type PeriodicidadeCondicionante = number | string;
export type StatusCondicionante = number | string;
export type NivelRiscoCondicionante = number | string;

export interface EvidenciaCondicionanteDto {
  readonly id: string;
  readonly condicionanteId?: string;
  readonly nomeArquivo?: string;
  readonly descricao?: string;
  readonly url?: string;
  readonly createdAtUtc?: string;
}

export interface CriarCondicionanteRequest {
  readonly licencaId: string;
  readonly codigoItem: string;
  readonly titulo: string;
  readonly descricao: string;
  readonly periodicidade: number;
  readonly dataVencimento: string;
  readonly responsavelTecnico?: string | null;
  readonly status?: number;
  readonly nivelRisco: number;
}

export interface AtualizarCondicionanteRequest {
  readonly licencaId: string;
  readonly codigoItem: string;
  readonly titulo: string;
  readonly descricao: string;
  readonly periodicidade: number;
  readonly dataVencimento: string;
  readonly responsavelTecnico?: string | null;
  readonly status: number;
  readonly nivelRisco: number;
}

export interface CondicionanteDto {
  readonly id: string;
  readonly licencaId: string;
  readonly codigoItem: string;
  readonly titulo: string;
  readonly descricao: string;
  readonly periodicidade: PeriodicidadeCondicionante;
  readonly dataVencimento: string;
  readonly responsavelTecnico?: string | null;
  readonly status: StatusCondicionante;
  readonly nivelRisco: NivelRiscoCondicionante;
  readonly quantidadeEvidencias: number;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
  readonly evidencias: readonly EvidenciaCondicionanteDto[];
}