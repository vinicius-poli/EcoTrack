import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, minLength, required } from '@angular/forms/signals';
import { Router, RouterLink } from '@angular/router';
import {
  CriarEmpreendimentoRequest,
  PorteEmpreendimento,
  StatusLicenciamento,
} from '../empreendimento.models';
import { EmpreendimentoService } from '../empreendimento.service';

export interface EmpreendimentoFormModel {
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
  imports: [FormField, RouterLink],
  selector: 'app-cadastrar-empreendimento',
  templateUrl: './cadastrar-empreendimento.component.html',
})
export class CadastrarEmpreendimentoComponent {
  private readonly empreendimentoService = inject(EmpreendimentoService);
  private readonly router = inject(Router);

  protected readonly enviandoFormulario = signal(false);
  protected readonly mensagemErro = signal('');

  protected readonly modeloEmpreendimento = signal<EmpreendimentoFormModel>({
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

  protected cadastrarEmpreendimento(event: SubmitEvent): void {
    event.preventDefault();

    const formulario = this.formularioEmpreendimento();

    if (formulario.invalid()) {
      formulario.markAsTouched();
      return;
    }

    const modelo = this.modeloEmpreendimento();

    const request: CriarEmpreendimentoRequest = {
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

    this.empreendimentoService.cadastrar(request).subscribe({
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

  private obterMensagemErroApi(resposta: HttpErrorResponse): string {
    const problem = resposta.error as ApiValidationProblemDetails | null;

    if (problem?.errors) {
      const mensagens = Object.values(problem.errors).flat();

      if (mensagens.length > 0) return mensagens.join('\n');
    }

    return (
      problem?.detail ??
      'Não foi possível cadastrar o empreendimento. Tente novamente em instantes.'
    );
  }
}