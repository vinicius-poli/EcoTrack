import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
  selector: 'app-detalhes-licenca',
  templateUrl: './detalhes-licenca.component.html',
})
export class DetalhesLicencaComponent {
  private readonly licencaService = inject(LicencaService);
  private readonly route = inject(ActivatedRoute);
  protected readonly authService = inject(AuthService);

  private readonly licencaId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly licenca = signal<LicencaDto | null>(null);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  constructor() {
    this.carregarLicenca();
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

  protected formatarDataHora(dataIso: string | null | undefined): string {
    if (!dataIso) return '—';

    const data = new Date(dataIso);
    if (Number.isNaN(data.getTime())) return dataIso;

    return data.toLocaleString('pt-BR', { timeZone: 'UTC' });
  }

  private carregarLicenca(): void {
    if (!this.licencaId) {
      this.naoEncontrado.set(true);
      this.carregando.set(false);
      return;
    }

    this.carregando.set(true);
    this.mensagemErro.set('');
    this.naoEncontrado.set(false);

    this.licencaService.selecionarPorId(this.licencaId).subscribe({
      next: (item) => {
        this.licenca.set(item);
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
      'Não foi possível carregar os detalhes da licença. Tente novamente em instantes.'
    );
  }
}