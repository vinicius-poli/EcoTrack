import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, minLength, required } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import {
  AtualizarEmpreendimentoRequest,
  EmpreendimentoDto,
  PorteEmpreendimento,
  StatusLicenciamento,
} from '../empreendimento.models';
import { EmpreendimentoService } from '../empreendimento.service';

export interface EditarEmpreendimentoFormModel {
  readonly razaoSocial: string;
  readonly nomeFantasia: string;
  readonly cnpj: string;
  readonly municipio: string;
  readonly uf: string;
  readonly porte: string;
  readonly statusLicenciamento: string;
}

interface ApiValidationProblemDetails {
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}

@Component({
  imports: [FormField, RouterLink, NavbarComponent],
  selector: 'app-editar-empreendimento',
  templateUrl: './editar-empreendimento.component.html',
})
export class EditarEmpreendimentoComponent {
  private readonly empreendimentoService = inject(EmpreendimentoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly empreendimentoId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly carregando = signal(true);
  protected readonly enviandoFormulario = signal(false);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly modeloEmpreendimento = signal<EditarEmpreendimentoFormModel>({
    razaoSocial: '',
    nomeFantasia: '',
    cnpj: '',
    municipio: '',
    uf: 'SC',
    porte: '1',
    statusLicenciamento: '1',
  });

  protected readonly formularioEmpreendimento = form(this.modeloEmpreendimento, (modelo) => {
    required(modelo.razaoSocial, { message: 'Informe a razão social.' });
    maxLength(modelo.razaoSocial, 150, {
      message: 'A razão social pode ter no máximo 150 caracteres.',
    });

    required(modelo.nomeFantasia, { message: 'Informe o nome fantasia.' });
    maxLength(modelo.nomeFantasia, 150, {
      message: 'O nome fantasia pode ter no máximo 150 caracteres.',
    });

    required(modelo.cnpj, { message: 'Informe o CNPJ.' });
    maxLength(modelo.cnpj, 18, { message: 'O CNPJ pode ter no máximo 18 caracteres.' });

    required(modelo.municipio, { message: 'Informe o município.' });
    maxLength(modelo.municipio, 100, {
      message: 'O município pode ter no máximo 100 caracteres.',
    });

    required(modelo.uf, { message: 'Informe a UF.' });
    minLength(modelo.uf, 2, { message: 'A UF deve ter 2 caracteres.' });
    maxLength(modelo.uf, 2, { message: 'A UF deve ter 2 caracteres.' });
  });

  protected readonly semPermissaoEscrita = computed(() => !this.authService.podeEscrever());

  constructor() {
    this.carregarEmpreendimento();
  }

  protected salvarAlteracoes(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.authService.podeEscrever() || !this.empreendimentoId) return;

    const formulario = this.formularioEmpreendimento();

    if (formulario.invalid()) {
      formulario.markAsTouched();
      return;
    }

    const modelo = this.modeloEmpreendimento();

    const request: AtualizarEmpreendimentoRequest = {
      razaoSocial: modelo.razaoSocial.trim(),
      nomeFantasia: modelo.nomeFantasia.trim(),
      cnpj: modelo.cnpj.trim(),
      municipio: modelo.municipio.trim(),
      uf: modelo.uf.trim().toUpperCase(),
      porte: Number(modelo.porte) as PorteEmpreendimento,
      statusLicenciamento: Number(modelo.statusLicenciamento) as StatusLicenciamento,
    };

    this.enviandoFormulario.set(true);
    this.mensagemErro.set('');

    this.empreendimentoService.atualizar(this.empreendimentoId, request).subscribe({
      next: () => {
        this.enviandoFormulario.set(false);
        void this.router.navigateByUrl('/empreendimentos');
      },
      error: (response: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErroApi(response));
        this.enviandoFormulario.set(false);
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
      next: (item: EmpreendimentoDto) => {
        this.modeloEmpreendimento.set({
          razaoSocial: item.razaoSocial,
          nomeFantasia: item.nomeFantasia,
          cnpj: item.cnpj,
          municipio: item.municipio,
          uf: item.uf,
          porte: this.normalizarPorteParaSelect(item.porte),
          statusLicenciamento: this.normalizarStatusParaSelect(item.statusLicenciamento),
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

  private normalizarPorteParaSelect(valor: string): string {
    const normalizado = valor.trim().toLowerCase();
    if (normalizado === '1' || normalizado.includes('micro') || normalizado.includes('pequeno')) {
      return '1';
    }
    if (normalizado === '2' || normalizado.includes('médio') || normalizado.includes('medio')) {
      return '2';
    }
    if (normalizado === '3' || normalizado.includes('grande')) {
      return '3';
    }
    return '1';
  }

  private normalizarStatusParaSelect(valor: string): string {
    const normalizado = valor.trim().toLowerCase();
    if (normalizado === '1' || normalizado.includes('regular')) {
      return '1';
    }
    if (normalizado === '2' || normalizado.includes('pendente')) {
      return '2';
    }
    if (normalizado === '3' || normalizado.includes('vencido')) {
      return '3';
    }
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
      'Não foi possível salvar as alterações do empreendimento. Tente novamente em instantes.'
    );
  }
}