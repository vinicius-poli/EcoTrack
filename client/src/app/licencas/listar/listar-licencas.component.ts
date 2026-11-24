import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../../auth/auth.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import { LicencaDto, StatusLicenca, TipoLicenca } from '../licenca.models';
import { LicencaService } from '../licenca.service';

interface ApiValidationProblemDetails {
  readonly detail?: string;
}

const ROTULOS_TIPO_LICENCA: Record<number, string> = {
  1: 'LP — Licença Prévia',
  2: 'LI — Licença de Instalação',
  3: 'LO — Licença de Operação',
};

const ROTULOS_STATUS_LICENCA: Record<number, string> = {
  1: 'Vigente',
  2: 'Em Renovação',
  3: 'Vencida',
  4: 'Suspensa / Cancelada',
};

@Component({
  imports: [RouterLink, NavbarComponent],
  selector: 'app-listar-licencas',
  templateUrl: './listar-licencas.component.html',
})
export class ListarLicencasComponent {
  private readonly licencaService = inject(LicencaService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly empreendimentoIdFiltro = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('empreendimentoId'))),
    { initialValue: null },
  );

  protected readonly licencas = signal<readonly LicencaDto[]>([]);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly termoBusca = signal('');

  protected readonly licencasFiltradas = computed(() => {
    const termo = this.termoBusca().trim().toLowerCase();
    if (!termo) return this.licencas();

    return this.licencas().filter((l) => {
      const tipoTexto = this.formatarTipo(l.tipo).toLowerCase();
      const statusTexto = this.formatarStatus(l.status).toLowerCase();
      const protocolo = (l.numeroProtocoloRenovacao ?? '').toLowerCase();

      return (
        l.numeroLicenca.toLowerCase().includes(termo) ||
        l.numeroProcesso.toLowerCase().includes(termo) ||
        l.orgaoEmissor.toLowerCase().includes(termo) ||
        tipoTexto.includes(termo) ||
        statusTexto.includes(termo) ||
        protocolo.includes(termo)
      );
    });
  });

  constructor() {
    effect(() => {
      const empreendimentoId = this.empreendimentoIdFiltro();
      this.carregarLicencas(empreendimentoId);
    });
  }

  protected atualizarBusca(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.termoBusca.set(input.value);
  }

  protected navegarParaCadastro(): void {
    if (!this.authService.podeEscrever()) return;

    const empreendimentoId = this.empreendimentoIdFiltro();
    if (empreendimentoId) {
      void this.router.navigate(['/licencas/cadastrar'], {
        queryParams: { empreendimentoId },
      });
      return;
    }

    void this.router.navigate(['/licencas/cadastrar']);
  }

  protected limparFiltroEmpreendimento(): void {
    void this.router.navigate(['/licencas']);
  }

  protected formatarTipo(tipo: TipoLicenca): string {
    if (typeof tipo === 'number') {
      return ROTULOS_TIPO_LICENCA[tipo] ?? `Tipo ${tipo}`;
    }

    const comoNumero = Number(tipo);
    if (!Number.isNaN(comoNumero) && ROTULOS_TIPO_LICENCA[comoNumero]) {
      return ROTULOS_TIPO_LICENCA[comoNumero];
    }

    return tipo;
  }

  protected formatarStatus(status: StatusLicenca): string {
    if (typeof status === 'number') {
      return ROTULOS_STATUS_LICENCA[status] ?? `Status ${status}`;
    }

    const comoNumero = Number(status);
    if (!Number.isNaN(comoNumero) && ROTULOS_STATUS_LICENCA[comoNumero]) {
      return ROTULOS_STATUS_LICENCA[comoNumero];
    }

    return status;
  }

  protected formatarData(dataIso: string | null | undefined): string {
    if (!dataIso) return '—';

    const data = new Date(dataIso);
    if (Number.isNaN(data.getTime())) return dataIso;

    return data.toLocaleDateString('pt-BR', { timeZone: 'UTC' });
  }

  private carregarLicencas(empreendimentoId?: string | null): void {
    this.carregando.set(true);
    this.mensagemErro.set('');

    this.licencaService.selecionarTodos(empreendimentoId).subscribe({
      next: (licencas) => {
        this.licencas.set(licencas);
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
      'Não foi possível carregar as licenças. Tente novamente em instantes.'
    );
  }
}