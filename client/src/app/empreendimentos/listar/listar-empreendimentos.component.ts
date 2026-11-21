import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EmpreendimentoDto } from '../empreendimento.models';
import { EmpreendimentoService } from '../empreendimento.service';

interface ApiValidationProblemDetails {
  readonly detail?: string;
}

@Component({
  imports: [RouterLink],
  selector: 'app-listar-empreendimentos',
  templateUrl: './listar-empreendimentos.component.html',
})
export class ListarEmpreendimentosComponent {
  private readonly empreendimentoService = inject(EmpreendimentoService);

  protected readonly empreendimentos = signal<readonly EmpreendimentoDto[]>([]);
  protected readonly carregando = signal(true);
  protected readonly mensagemErro = signal('');
  protected readonly termoBusca = signal('');

  protected readonly empreendimentosFiltrados = computed(() => {
    const termo = this.termoBusca().trim().toLowerCase();
    if (!termo) return this.empreendimentos();

    return this.empreendimentos().filter(
      (e) =>
        e.razaoSocial.toLowerCase().includes(termo) ||
        e.nomeFantasia.toLowerCase().includes(termo) ||
        e.municipio.toLowerCase().includes(termo) ||
        e.cnpj.includes(termo)
    );
  });

  constructor() {
    this.carregarEmpreendimentos();
  }

  protected atualizarBusca(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    this.termoBusca.set(input.value);
  }

  private carregarEmpreendimentos(): void {
    this.carregando.set(true);
    this.mensagemErro.set('');

    this.empreendimentoService.selecionarTodos().subscribe({
      next: (empreendimentos) => {
        this.empreendimentos.set(empreendimentos);
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
      'Não foi possível carregar os empreendimentos. Tente novamente em instantes.'
    );
  }
}