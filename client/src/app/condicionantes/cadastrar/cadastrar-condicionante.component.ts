import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { LicencaDto } from '../../licencas/licenca.models';
import { LicencaService } from '../../licencas/licenca.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import { CriarCondicionanteRequest } from '../condicionante.models';
import { CondicionanteService } from '../condicionante.service';

export interface CondicionanteFormModel {
  readonly licencaId: string;
  readonly codigoItem: string;
  readonly titulo: string;
  readonly descricao: string;
  readonly periodicidade: string;
  readonly dataVencimento: string;
  readonly responsavelTecnico: string;
  readonly status: string;
}

interface ApiValidationProblemDetails {
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}

@Component({
  imports: [FormField, RouterLink, NavbarComponent],
  selector: 'app-cadastrar-condicionante',
  templateUrl: './cadastrar-condicionante.component.html',
})
export class CadastrarCondicionanteComponent {
  private readonly condicionanteService = inject(CondicionanteService);
  private readonly licencaService = inject(LicencaService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly licencaIdQuery =
    this.route.snapshot.queryParamMap.get('licencaId') ?? '';

  protected readonly licencas = signal<readonly LicencaDto[]>([]);
  protected readonly carregandoLicencas = signal(true);
  protected readonly enviandoFormulario = signal(false);
  protected readonly mensagemErro = signal('');

  protected readonly semPermissaoEscrita = computed(() => !this.authService.podeEscrever());

  protected readonly modeloCondicionante = signal<CondicionanteFormModel>({
    licencaId: this.licencaIdQuery,
    codigoItem: '',
    titulo: '',
    descricao: '',
    periodicidade: '1',
    dataVencimento: '',
    responsavelTecnico: '',
    status: '1',
  });

  protected readonly formularioCondicionante = form(this.modeloCondicionante, (modelo) => {
    required(modelo.licencaId, { message: 'Selecione a licença ambiental.' });

    required(modelo.codigoItem, { message: 'Informe o código do item.' });
    maxLength(modelo.codigoItem, 40, {
      message: 'O código do item pode ter no máximo 40 caracteres.',
    });

    required(modelo.titulo, { message: 'Informe o título da condicionante.' });
    maxLength(modelo.titulo, 160, {
      message: 'O título pode ter no máximo 160 caracteres.',
    });

    required(modelo.descricao, { message: 'Informe a descrição da condicionante.' });
    maxLength(modelo.descricao, 1500, {
      message: 'A descrição pode ter no máximo 1500 caracteres.',
    });

    required(modelo.dataVencimento, { message: 'Informe a data de vencimento.' });

    maxLength(modelo.responsavelTecnico, 120, {
      message: 'O responsável técnico pode ter no máximo 120 caracteres.',
    });
  });

  constructor() {
    this.carregarLicencas();
  }

  protected cadastrarCondicionante(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.authService.podeEscrever()) return;

    const formulario = this.formularioCondicionante();

    if (formulario.invalid()) {
      formulario.markAsTouched();
      return;
    }

    const modelo = this.modeloCondicionante();
    const responsavelLimpo = modelo.responsavelTecnico.trim();

    const request: CriarCondicionanteRequest = {
      licencaId: modelo.licencaId,
      codigoItem: modelo.codigoItem.trim(),
      titulo: modelo.titulo.trim(),
      descricao: modelo.descricao.trim(),
      periodicidade: Number(modelo.periodicidade),
      dataVencimento: modelo.dataVencimento,
      responsavelTecnico: responsavelLimpo || null,
      status: Number(modelo.status),
      nivelRisco: 4,
    };

    this.enviandoFormulario.set(true);
    this.mensagemErro.set('');

    this.condicionanteService.cadastrar(request).subscribe({
      next: () => {
        this.enviandoFormulario.set(false);
        if (this.licencaIdQuery) {
          void this.router.navigate(['/condicionantes'], {
            queryParams: { licencaId: this.licencaIdQuery },
          });
          return;
        }
        void this.router.navigateByUrl('/condicionantes');
      },
      error: (response: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErroApi(response));
        this.enviandoFormulario.set(false);
      },
    });
  }

  private carregarLicencas(): void {
    this.carregandoLicencas.set(true);

    this.licencaService.selecionarTodos().subscribe({
      next: (lista) => {
        this.licencas.set(lista);
        this.carregandoLicencas.set(false);
      },
      error: () => {
        this.carregandoLicencas.set(false);
      },
    });
  }

  private obterMensagemErroApi(resposta: HttpErrorResponse): string {
    const problem = resposta.error as ApiValidationProblemDetails | null;

    if (problem?.errors) {
      const mensagens = Object.values(problem.errors).flat();

      if (mensagens.length > 0) return mensagens.join('\n');
    }

    return (
      problem?.detail ??
      'Não foi possível cadastrar a condicionante. Tente novamente em instantes.'
    );
  }
}