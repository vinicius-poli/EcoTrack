import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { LicencaDto } from '../../licencas/licenca.models';
import { LicencaService } from '../../licencas/licenca.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import {
  AtualizarCondicionanteRequest,
  CondicionanteDto,
  PeriodicidadeCondicionante,
  StatusCondicionante,
} from '../condicionante.models';
import { CondicionanteService } from '../condicionante.service';

export interface EditarCondicionanteFormModel {
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
  selector: 'app-editar-condicionante',
  templateUrl: './editar-condicionante.component.html',
})
export class EditarCondicionanteComponent {
  private readonly condicionanteService = inject(CondicionanteService);
  private readonly licencaService = inject(LicencaService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly condicionanteId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly licencas = signal<readonly LicencaDto[]>([]);
  protected readonly carregandoLicencas = signal(true);
  protected readonly carregando = signal(true);
  protected readonly enviandoFormulario = signal(false);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly semPermissaoEscrita = computed(() => !this.authService.podeEscrever());

  protected readonly modeloCondicionante = signal<EditarCondicionanteFormModel>({
    licencaId: '',
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
    this.carregarCondicionante();
  }

  protected salvarAlteracoes(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.authService.podeEscrever() || !this.condicionanteId) return;

    const formulario = this.formularioCondicionante();

    if (formulario.invalid()) {
      formulario.markAsTouched();
      return;
    }

    const modelo = this.modeloCondicionante();
    const responsavelLimpo = modelo.responsavelTecnico.trim();

    const request: AtualizarCondicionanteRequest = {
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

    this.condicionanteService.atualizar(this.condicionanteId, request).subscribe({
      next: () => {
        this.enviandoFormulario.set(false);
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
      next: (item: CondicionanteDto) => {
        this.modeloCondicionante.set({
          licencaId: item.licencaId,
          codigoItem: item.codigoItem,
          titulo: item.titulo,
          descricao: item.descricao,
          periodicidade: this.normalizarPeriodicidadeParaSelect(item.periodicidade),
          dataVencimento: this.formatarDataParaInput(item.dataVencimento),
          responsavelTecnico: item.responsavelTecnico ?? '',
          status: this.normalizarStatusParaSelect(item.status),
        });
        this.carregando.set(false);
      },
      error: (resposta: HttpErrorResponse) => {
        if (resposta.status === 404) {
          this.naoEncontrado.set(true);
        } else {
          this.mensagemErro.set(this.obterMensagemErroApi(resposta));
        }
        this.carregando.set(false);
      },
    });
  }

  private formatarDataParaInput(dataIso: string | null | undefined): string {
    if (!dataIso) return '';
    return dataIso.slice(0, 10);
  }

  private normalizarPeriodicidadeParaSelect(periodicidade: PeriodicidadeCondicionante): string {
    const valor = String(periodicidade).trim().toLowerCase();
    if (valor === '1' || valor.includes('unica') || valor.includes('única')) return '1';
    if (valor === '2' || valor.includes('mensal')) return '2';
    if (valor === '3' || valor.includes('trimestral')) return '3';
    if (valor === '4' || valor.includes('semestral')) return '4';
    if (valor === '5' || valor.includes('anual')) return '5';
    if (valor === '6' || valor.includes('continua') || valor.includes('contínua')) return '6';
    return '1';
  }

  private normalizarStatusParaSelect(status: StatusCondicionante): string {
    const valor = String(status).trim().toLowerCase();
    if (valor === '1' || valor.includes('pendente')) return '1';
    if (valor === '2' || valor.includes('andamento')) return '2';
    if (valor === '3' || valor.includes('concluida') || valor.includes('concluída')) return '3';
    return '1';
  }

  private obterMensagemErroApi(resposta: HttpErrorResponse): string {
    const problem = resposta.error as ApiValidationProblemDetails | null;

    if (problem?.errors) {
      const mensagens = Object.values(problem.errors).flat();

      if (mensagens.length > 0) return mensagens.join('\n');
    }

    return (
      problem?.detail ??
      'Não foi possível salvar as alterações da condicionante. Tente novamente em instantes.'
    );
  }
}