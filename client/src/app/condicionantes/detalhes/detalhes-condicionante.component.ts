import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import {
  CondicionanteDto,
  EvidenciaCondicionanteDto,
  NivelRiscoCondicionante,
  PeriodicidadeCondicionante,
  StatusCondicionante,
} from '../condicionante.models';
import { CondicionanteService } from '../condicionante.service';

interface ApiValidationProblemDetails {
  readonly detail?: string;
}

const ROTULOS_STATUS: Record<number, string> = {
  1: 'Pendente',
  2: 'Em Andamento',
  3: 'Concluída',
};

const ROTULOS_NIVEL_RISCO: Record<number, string> = {
  1: 'Concluído',
  2: 'Crítico',
  3: 'Alerta',
  4: 'Normal',
};

const ROTULOS_PERIODICIDADE: Record<number, string> = {
  1: 'Única',
  2: 'Mensal',
  3: 'Trimestral',
  4: 'Semestral',
  5: 'Anual',
  6: 'Contínua',
};

@Component({
  imports: [RouterLink, NavbarComponent],
  selector: 'app-detalhes-condicionante',
  templateUrl: './detalhes-condicionante.component.html',
})
export class DetalhesCondicionanteComponent {
  private readonly condicionanteService = inject(CondicionanteService);
  private readonly route = inject(ActivatedRoute);
  protected readonly authService = inject(AuthService);

  private readonly condicionanteId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly condicionante = signal<CondicionanteDto | null>(null);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly arquivoEvidencia = signal<File | null>(null);
  protected readonly descricaoEvidencia = signal('');
  protected readonly enviandoEvidencia = signal(false);
  protected readonly mensagemEvidenciaErro = signal('');
  protected readonly mensagemEvidenciaSucesso = signal('');
  protected readonly inputArquivoChave = signal(0);

  protected readonly baixandoEvidenciaId = signal<string | null>(null);
  protected readonly excluindoEvidenciaId = signal<string | null>(null);

  constructor() {
    this.carregarCondicionante();
  }

  protected aoSelecionarArquivo(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const arquivo = input.files && input.files.length > 0 ? input.files[0] : null;

    this.mensagemEvidenciaErro.set('');
    this.mensagemEvidenciaSucesso.set('');
    this.arquivoEvidencia.set(arquivo);
  }

  protected atualizarDescricaoEvidencia(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.descricaoEvidencia.set(input.value);
  }

  protected enviarEvidencia(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.authService.podeEscrever() || !this.condicionanteId || this.enviandoEvidencia()) {
      return;
    }

    const arquivo = this.arquivoEvidencia();
    if (!arquivo) {
      this.mensagemEvidenciaErro.set('Selecione um arquivo antes de enviar a evidência.');
      return;
    }

    this.enviandoEvidencia.set(true);
    this.mensagemEvidenciaErro.set('');
    this.mensagemEvidenciaSucesso.set('');

    this.condicionanteService
      .adicionarEvidencia(this.condicionanteId, arquivo, this.descricaoEvidencia())
      .subscribe({
        next: () => {
          this.enviandoEvidencia.set(false);
          this.arquivoEvidencia.set(null);
          this.descricaoEvidencia.set('');
          this.inputArquivoChave.update((chave) => chave + 1);
          this.mensagemEvidenciaSucesso.set('Evidência enviada com sucesso.');
          this.carregarCondicionante();
        },
        error: (resposta: HttpErrorResponse) => {
          this.mensagemEvidenciaErro.set(this.obterMensagemErro(resposta));
          this.enviandoEvidencia.set(false);
        },
      });
  }

  protected baixarEvidencia(evidencia: EvidenciaCondicionanteDto): void {
    if (!this.condicionanteId || this.baixandoEvidenciaId()) return;

    this.baixandoEvidenciaId.set(evidencia.id);
    this.mensagemEvidenciaErro.set('');

    this.condicionanteService.baixarEvidencia(this.condicionanteId, evidencia.id).subscribe({
      next: (blob) => {
        const nomePadrao = evidencia.nomeArquivo || `evidencia-${evidencia.id}`;
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = nomePadrao;
        link.click();
        URL.revokeObjectURL(url);
        this.baixandoEvidenciaId.set(null);
      },
      error: (resposta: HttpErrorResponse) => {
        this.mensagemEvidenciaErro.set(this.obterMensagemErro(resposta));
        this.baixandoEvidenciaId.set(null);
      },
    });
  }

  protected excluirEvidencia(evidenciaId: string): void {
    if (!this.authService.podeExcluir() || !this.condicionanteId || this.excluindoEvidenciaId()) {
      return;
    }

    this.excluindoEvidenciaId.set(evidenciaId);
    this.mensagemEvidenciaErro.set('');
    this.mensagemEvidenciaSucesso.set('');

    this.condicionanteService.excluirEvidencia(this.condicionanteId, evidenciaId).subscribe({
      next: () => {
        this.excluindoEvidenciaId.set(null);
        this.mensagemEvidenciaSucesso.set('Evidência removida com sucesso.');
        this.carregarCondicionante();
      },
      error: (resposta: HttpErrorResponse) => {
        this.mensagemEvidenciaErro.set(this.obterMensagemErro(resposta));
        this.excluindoEvidenciaId.set(null);
      },
    });
  }

  protected formatarStatus(status: StatusCondicionante): string {
    return this.mapearEnum(status, ROTULOS_STATUS, 'Status');
  }

  protected formatarNivelRisco(risco: NivelRiscoCondicionante): string {
    return this.mapearEnum(risco, ROTULOS_NIVEL_RISCO, 'Risco');
  }

  protected formatarPeriodicidade(periodicidade: PeriodicidadeCondicionante): string {
    return this.mapearEnum(periodicidade, ROTULOS_PERIODICIDADE, 'Periodicidade');
  }

  protected classeNivelRisco(risco: NivelRiscoCondicionante): string {
    const valor = String(risco).trim().toLowerCase();

    if (valor === '1' || valor.includes('concluido') || valor.includes('concluído')) {
      return 'border-sky-200 bg-sky-50 text-sky-800';
    }
    if (valor === '2' || valor.includes('critico') || valor.includes('crítico')) {
      return 'border-red-200 bg-red-50 text-red-700';
    }
    if (valor === '3' || valor.includes('alerta')) {
      return 'border-amber-200 bg-amber-50 text-amber-800';
    }
    return 'border-emerald-200 bg-emerald-50 text-emerald-800';
  }

  protected formatarData(dataIso: string | null | undefined): string {
    if (!dataIso) return '—';

    const data = new Date(dataIso);
    if (Number.isNaN(data.getTime())) return dataIso;

    return data.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
  }

  protected formatarDataHora(dataIso: string | null | undefined): string {
    if (!dataIso) return '—';

    const data = new Date(dataIso);
    if (Number.isNaN(data.getTime())) return dataIso;

    return data.toLocaleString('pt-BR', { timeZone: 'UTC' });
  }

  private mapearEnum(
    valor: number | string,
    mapa: Record<number, string>,
    prefixo: string,
  ): string {
    if (typeof valor === 'number') {
      return mapa[valor] ?? `${prefixo} ${valor}`;
    }

    const comoNumero = Number(valor);
    if (!Number.isNaN(comoNumero) && mapa[comoNumero]) {
      return mapa[comoNumero];
    }

    return valor;
  }

  private carregarCondicionante(): void {
    if (!this.condicionanteId) {
      this.naoEncontrado.set(true);
      this.carregando.set(false);
      return;
    }

    this.carregando.set(true);
    this.mensagemErro.set('');
    this.naoEncontrado.set(false);

    this.condicionanteService.selecionarPorId(this.condicionanteId).subscribe({
      next: (item) => {
        this.condicionante.set(item);
        this.carregando.set(false);
      },
      error: (resposta: HttpErrorResponse) => {
        if (resposta.status === 404) {
          this.naoEncontrado.set(true);
        } else {
          this.mensagemErro.set(this.obterMensagemErro(resposta));
        }
        this.carregando.set(false);
      },
    });
  }

  private obterMensagemErro(response: HttpErrorResponse): string {
    const problem = response.error as ApiValidationProblemDetails | null;

    return (
      problem?.detail ??
      'Não foi possível processar a solicitação. Tente novamente em instantes.'
    );
  }
}