import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import {
  CondicionanteDto,
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
  selector: 'app-excluir-condicionante',
  templateUrl: './excluir-condicionante.component.html',
})
export class ExcluirCondicionanteComponent {
  private readonly condicionanteService = inject(CondicionanteService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly condicionanteId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly condicionante = signal<CondicionanteDto | null>(null);
  protected readonly carregando = signal(true);
  protected readonly excluindo = signal(false);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly semPermissaoExclusao = computed(() => !this.authService.podeExcluir());

  constructor() {
    this.carregarCondicionante();
  }

  protected confirmarExclusao(): void {
    if (!this.authService.podeExcluir() || !this.condicionanteId || this.excluindo()) {
      return;
    }

    this.excluindo.set(true);
    this.mensagemErro.set('');

    this.condicionanteService.excluir(this.condicionanteId).subscribe({
      next: () => {
        this.excluindo.set(false);
        void this.router.navigateByUrl('/condicionantes');
      },
      error: (resposta: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErro(resposta));
        this.excluindo.set(false);
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
      'Não foi possível excluir a condicionante. Tente novamente em instantes.'
    );
  }
}