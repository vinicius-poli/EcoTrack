import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { LicencaService } from '../../licencas/licenca.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import { EmpreendimentoDto } from '../empreendimento.models';
import { EmpreendimentoService } from '../empreendimento.service';

interface ApiValidationProblemDetails {
  readonly detail?: string;
}

@Component({
  imports: [RouterLink, NavbarComponent],
  selector: 'app-detalhes-empreendimento',
  templateUrl: './detalhes-empreendimento.component.html',
})
export class DetalhesEmpreendimentoComponent {
  private readonly empreendimentoService = inject(EmpreendimentoService);
  private readonly licencaService = inject(LicencaService);
  private readonly route = inject(ActivatedRoute);
  protected readonly authService = inject(AuthService);

  private readonly empreendimentoId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly empreendimento = signal<EmpreendimentoDto | null>(null);
  protected readonly quantidadeLicencas = signal(0);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  constructor() {
    this.carregarEmpreendimento();
  }

  private carregarEmpreendimento(): void {
    if (!this.empreendimentoId) {
      this.naoEncontrado.set(true);
      this.carregando.set(false);
      return;
    }

    this.carregando.set(true);
    this.mensagemErro.set('');
    this.naoEncontrado.set(false);
    this.quantidadeLicencas.set(0);

    this.empreendimentoService.selecionarPorId(this.empreendimentoId).subscribe({
      next: (item) => {
        this.empreendimento.set(item);
        this.carregarQuantidadeLicencas();
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

  private carregarQuantidadeLicencas(): void {
    this.licencaService.selecionarTodos(this.empreendimentoId).subscribe({
      next: (licencas) => {
        const filtradas = (licencas ?? []).filter(
          (l) => !l.empreendimentoId || l.empreendimentoId === this.empreendimentoId,
        );
        this.quantidadeLicencas.set(filtradas.length);
      },
      error: () => {
        this.quantidadeLicencas.set(0);
      },
    });
  }

  private obterMensagemErro(response: HttpErrorResponse): string {
    const problem = response.error as ApiValidationProblemDetails | null;

    return (
      problem?.detail ??
      'Não foi possível carregar os detalhes do empreendimento. Tente novamente em instantes.'
    );
  }
}