import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
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
  selector: 'app-listar-condicionantes',
  templateUrl: './listar-condicionantes.component.html',
})
export class ListarCondicionantesComponent {
  private readonly condicionanteService = inject(CondicionanteService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  protected readonly licencaIdFiltro = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('licencaId'))),
    { initialValue: null },
  );

  protected readonly condicionantes = signal<readonly CondicionanteDto[]>([]);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly termoBusca = signal('');

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

  protected readonly condicionantesFiltradas = computed(() => {
    const termo = this.termoBusca().trim().toLowerCase();
    if (!termo) return this.condicionantes();

    return this.condicionantes().filter((c) => {
      const statusTexto = this.formatarStatus(c.status).toLowerCase();
      const riscoTexto = this.formatarNivelRisco(c.nivelRisco).toLowerCase();
      const periodicidadeTexto = this.formatarPeriodicidade(c.periodicidade).toLowerCase();

      return (
        c.codigoItem.toLowerCase().includes(termo) ||
        c.titulo.toLowerCase().includes(termo) ||
        c.descricao.toLowerCase().includes(termo) ||
        (c.responsavelTecnico?.toLowerCase().includes(termo) ?? false) ||
        statusTexto.includes(termo) ||
        riscoTexto.includes(termo) ||
        periodicidadeTexto.includes(termo)
      );
    });
  });

  constructor() {
    effect(() => {
      const licencaId = this.licencaIdFiltro();
      this.carregarCondicionantes(licencaId);
    });
  }

  protected atualizarBusca(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.termoBusca.set(input.value);
  }

  protected navegarParaCadastro(): void {
    if (!this.authService.podeEscrever()) return;

    const licencaId = this.licencaIdFiltro();
    if (licencaId) {
      void this.router.navigate(['/condicionantes/cadastrar'], {
        queryParams: { licencaId },
      });
      return;
    }

    void this.router.navigate(['/condicionantes/cadastrar']);
  }

  protected limparFiltroLicenca(): void {
    void this.router.navigate(['/condicionantes']);
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

  private carregarCondicionantes(licencaId?: string | null): void {
    this.carregando.set(true);
    this.mensagemErro.set('');

    this.condicionanteService.selecionarTodos(licencaId).subscribe({
      next: (lista) => {
        this.condicionantes.set(lista);
        this.carregando.set(false);
      },
      error: (resposta: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErro(resposta));
        this.carregando.set(false);
      },
    });
  }

  private obterMensagemErro(response: HttpErrorResponse): string {
    const problem = response.error as ApiValidationProblemDetails | null;

    return (
      problem?.detail ??
      'Não foi possível carregar as condicionantes. Tente novamente em instantes.'
    );
  }
}