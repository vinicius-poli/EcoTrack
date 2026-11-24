import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { form, FormField, maxLength, min, required } from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { EmpreendimentoDto } from '../../empreendimentos/empreendimento.models';
import { EmpreendimentoService } from '../../empreendimentos/empreendimento.service';
import { NavbarComponent } from '../../shared/navbar/navbar.component';
import {
  AtualizarLicencaRequest,
  LicencaDto,
  StatusLicenca,
  TipoLicenca,
} from '../licenca.models';
import { LicencaService } from '../licenca.service';

export interface EditarLicencaFormModel {
  readonly empreendimentoId: string;
  readonly numeroProcesso: string;
  readonly numeroLicenca: string;
  readonly orgaoEmissor: string;
  readonly tipo: string;
  readonly dataEmissao: string;
  readonly dataValidade: string;
  readonly diasAntecedenciaRenovacao: number | null;
  readonly status: string;
  readonly numeroProtocoloRenovacao: string;
  readonly dataProtocoloRenovacao: string;
}

interface ApiValidationProblemDetails {
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}

@Component({
  imports: [FormField, RouterLink, NavbarComponent],
  selector: 'app-editar-licenca',
  templateUrl: './editar-licenca.component.html',
})
export class EditarLicencaComponent {
  private readonly licencaService = inject(LicencaService);
  private readonly empreendimentoService = inject(EmpreendimentoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  protected readonly authService = inject(AuthService);

  private readonly licencaId = this.route.snapshot.paramMap.get('id') ?? '';

  protected readonly empreendimentos = signal<readonly EmpreendimentoDto[]>([]);
  protected readonly carregandoEmpreendimentos = signal(true);
  protected readonly carregando = signal(true);
  protected readonly enviandoFormulario = signal(false);
  protected readonly mensagemErro = signal('');
  protected readonly naoEncontrado = signal(false);

  protected readonly semPermissaoEscrita = computed(() => !this.authService.podeEscrever());

  protected readonly modeloLicenca = signal<EditarLicencaFormModel>({
    empreendimentoId: '',
    numeroProcesso: '',
    numeroLicenca: '',
    orgaoEmissor: '',
    tipo: '3',
    dataEmissao: '',
    dataValidade: '',
    diasAntecedenciaRenovacao: 120,
    status: '1',
    numeroProtocoloRenovacao: '',
    dataProtocoloRenovacao: '',
  });

  protected readonly formularioLicenca = form(this.modeloLicenca, (modelo) => {
    required(modelo.empreendimentoId, { message: 'Selecione o empreendimento.' });

    required(modelo.numeroProcesso, { message: 'Informe o número do processo.' });
    maxLength(modelo.numeroProcesso, 60, {
      message: 'O número do processo pode ter no máximo 60 caracteres.',
    });

    required(modelo.numeroLicenca, { message: 'Informe o número da licença.' });
    maxLength(modelo.numeroLicenca, 60, {
      message: 'O número da licença pode ter no máximo 60 caracteres.',
    });

    required(modelo.orgaoEmissor, { message: 'Informe o órgão emissor.' });
    maxLength(modelo.orgaoEmissor, 120, {
      message: 'O órgão emissor pode ter no máximo 120 caracteres.',
    });

    required(modelo.dataEmissao, { message: 'Informe a data de emissão.' });
    required(modelo.dataValidade, { message: 'Informe a data de validade.' });

    required(modelo.diasAntecedenciaRenovacao, {
      message: 'Informe os dias de antecedência para renovação.',
    });
    min(modelo.diasAntecedenciaRenovacao, 0, {
      message: 'A antecedência não pode ser negativa.',
    });
  });

  constructor() {
    this.carregarEmpreendimentos();
    this.carregarLicenca();
  }

  protected salvarAlteracoes(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.authService.podeEscrever() || !this.licencaId) return;

    const formulario = this.formularioLicenca();

    if (formulario.invalid()) {
      formulario.markAsTouched();
      return;
    }

    const modelo = this.modeloLicenca();

    if (modelo.dataValidade < modelo.dataEmissao) {
      this.mensagemErro.set('A data de validade deve ser igual ou posterior à data de emissão.');
      return;
    }

    const protocoloLimpo = modelo.numeroProtocoloRenovacao.trim();

    const request: AtualizarLicencaRequest = {
      empreendimentoId: modelo.empreendimentoId,
      numeroProcesso: modelo.numeroProcesso.trim(),
      numeroLicenca: modelo.numeroLicenca.trim(),
      orgaoEmissor: modelo.orgaoEmissor.trim(),
      tipo: Number(modelo.tipo),
      dataEmissao: modelo.dataEmissao,
      dataValidade: modelo.dataValidade,
      diasAntecedenciaRenovacao: modelo.diasAntecedenciaRenovacao ?? 0,
      status: Number(modelo.status),
      numeroProtocoloRenovacao: protocoloLimpo || null,
      dataProtocoloRenovacao: modelo.dataProtocoloRenovacao || null,
    };

    this.enviandoFormulario.set(true);
    this.mensagemErro.set('');

    this.licencaService.atualizar(this.licencaId, request).subscribe({
      next: () => {
        this.enviandoFormulario.set(false);
        void this.router.navigateByUrl('/licencas');
      },
      error: (response: HttpErrorResponse) => {
        this.mensagemErro.set(this.obterMensagemErroApi(response));
        this.enviandoFormulario.set(false);
      },
    });
  }

  private carregarEmpreendimentos(): void {
    this.carregandoEmpreendimentos.set(true);

    this.empreendimentoService.selecionarTodos().subscribe({
      next: (lista) => {
        this.empreendimentos.set(lista);
        this.carregandoEmpreendimentos.set(false);
      },
      error: () => {
        this.carregandoEmpreendimentos.set(false);
      },
    });
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
      next: (item: LicencaDto) => {
        this.modeloLicenca.set({
          empreendimentoId: item.empreendimentoId,
          numeroProcesso: item.numeroProcesso,
          numeroLicenca: item.numeroLicenca,
          orgaoEmissor: item.orgaoEmissor,
          tipo: this.normalizarTipoParaSelect(item.tipo),
          dataEmissao: this.formatarDataParaInput(item.dataEmissao),
          dataValidade: this.formatarDataParaInput(item.dataValidade),
          diasAntecedenciaRenovacao: item.diasAntecedenciaRenovacao,
          status: this.normalizarStatusParaSelect(item.status),
          numeroProtocoloRenovacao: item.numeroProtocoloRenovacao ?? '',
          dataProtocoloRenovacao: this.formatarDataParaInput(item.dataProtocoloRenovacao),
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

  private normalizarTipoParaSelect(tipo: TipoLicenca): string {
    const normalizado = String(tipo).trim().toLowerCase();
    if (normalizado === '1' || normalizado.includes('prévia') || normalizado.includes('previa') || normalizado === 'lp') {
      return '1';
    }
    if (normalizado === '2' || normalizado.includes('instalação') || normalizado.includes('instalacao') || normalizado === 'li') {
      return '2';
    }
    if (normalizado === '3' || normalizado.includes('operação') || normalizado.includes('operacao') || normalizado === 'lo') {
      return '3';
    }
    return '3';
  }

  private normalizarStatusParaSelect(status: StatusLicenca): string {
    const normalizado = String(status).trim().toLowerCase();
    if (normalizado === '1' || normalizado.includes('vigente')) {
      return '1';
    }
    if (normalizado === '2' || normalizado.includes('renov')) {
      return '2';
    }
    if (normalizado === '3' || normalizado.includes('vencida')) {
      return '3';
    }
    if (normalizado === '4' || normalizado.includes('suspensa') || normalizado.includes('cancelada')) {
      return '4';
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
      'Não foi possível salvar as alterações da licença. Tente novamente em instantes.'
    );
  }
}