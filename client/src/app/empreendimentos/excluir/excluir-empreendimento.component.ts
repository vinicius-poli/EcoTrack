import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import { EmpreendimentoDto } from '../empreendimento.models';
import { EmpreendimentoService } from '../empreendimento.service';

interface ApiValidationProblemDetails {
  readonly detail?: string;
}

@Component({
  imports: [RouterLink, NavbarComponent],
  selector: 'app-excluir-empreendimento',
  templateUrl: './excluir-empreendimento.component.html',
})
export class ExcluirEmpreendimentoComponent {
  private readonly empreendimentoService = inject(EmpreendimentoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly empreendimentoId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly empreendimento = signal<EmpreendimentoDto | null>(null);
  protected readonly carregando = signal(true);
  protected readonly excluindo = signal(false);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly semPermissaoExclusao = computed(() => !this.authService.podeExcluir());

  constructor() {
    this.carregarEmpreendimento();
  }

  protected confirmarExclusao(): void {
    if (!this.authService.podeExcluir() || !this.empreendimentoId || this.excluindo()) {
      return;
    }

    this.excluindo.set(true);
    this.mensagemErro.set('');

    this.empreendimentoService.excluir(this.empreendimentoId).subscribe({
      next: () => {
        this.excluindo.set(false);
        void this.router.navigateByUrl('/empreendimentos');
      },
      error: (resposta: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErro(resposta));
        this.excluindo.set(false);
      },
    });
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

    this.empreendimentoService.selecionarPorId(this.empreendimentoId).subscribe({
      next: (item) => {
        this.empreendimento.set(item);
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
      'Não foi possível concluir a operação com o empreendimento. Tente novamente em instantes.'
    );
  }
}