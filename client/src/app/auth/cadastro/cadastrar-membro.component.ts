import { HttpErrorResponse } from '@angular/common/http';
import {
  afterNextRender,
  Component,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { email, form, FormField, minLength, pattern, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import {
  ApiValidationProblemDetails,
  CadastrarMembroRequest,
  PerfilMembroTenant,
  UsuarioDto,
} from '../auth.models';
import { AuthService } from '../auth.service';

interface CadastrarMembroFormModel {
  readonly nome: string;
  readonly email: string;
  readonly senha: string;
  readonly perfil: PerfilMembroTenant | '';
}

@Component({
  imports: [FormField, RouterLink],
  selector: 'app-cadastrar-membro',
  templateUrl: './cadastrar-membro.component.html',
})
export class CadastrarMembroComponent {
  private readonly authService = inject(AuthService);
  protected readonly permissao = this.authService;

  private readonly tituloPagina = viewChild<ElementRef<HTMLHeadingElement>>('tituloPagina');

  protected readonly modeloFormulario = signal<CadastrarMembroFormModel>({
    nome: '',
    email: '',
    senha: '',
    perfil: '',
  });

  protected readonly formularioMembro = form(this.modeloFormulario, (modelo) => {
    required(modelo.nome, { message: 'Informe o nome do membro.' });
    pattern(modelo.nome, /\S/, {
      message: 'O nome não pode conter apenas espaços.',
    });
    required(modelo.email, { message: 'Informe o e-mail do membro.' });
    email(modelo.email, { message: 'Informe um e-mail válido.' });
    required(modelo.senha, { message: 'Informe uma senha inicial.' });
    minLength(modelo.senha, 8, { message: 'A senha precisa ter pelo menos 8 caracteres.' });
    pattern(modelo.senha, /(?=.*\d)(?=.*[^a-zA-Z0-9]).+/, {
      message: 'Inclua pelo menos um número e um caractere especial.',
    });
    required(modelo.perfil, { message: 'Selecione o perfil (Gestor ou Analista).' });
  });

  protected readonly mensagemErro = signal('');
  protected readonly membroCriado = signal<UsuarioDto | null>(null);
  protected readonly enviandoFormulario = signal(false);

  constructor() {
    afterNextRender(() => this.tituloPagina()?.nativeElement.focus());
  }

  protected enviarFormulario(event: SubmitEvent): void {
    event.preventDefault();

    if (!this.permissao.ehAdministrador()) {
      this.mensagemErro.set('Apenas usuários com perfil Administrador podem adicionar membros.');
      return;
    }

    if (this.formularioMembro().invalid()) {
      this.formularioMembro().markAsTouched();
      return;
    }

    const valor = this.modeloFormulario();
    const payload: CadastrarMembroRequest = {
      nome: valor.nome.trim(),
      email: valor.email.trim(),
      senha: valor.senha,
      perfil: valor.perfil,
    };

    this.mensagemErro.set('');
    this.membroCriado.set(null);
    this.enviandoFormulario.set(true);

    this.authService.cadastrarMembro(payload).subscribe({
      next: (usuario) => {
        this.enviandoFormulario.set(false);
        this.membroCriado.set(usuario);
        this.modeloFormulario.set({
          nome: '',
          email: '',
          senha: '',
          perfil: '',
        });
        this.formularioMembro().reset();
      },
      error: (response: HttpErrorResponse) => {
        this.enviandoFormulario.set(false);
        this.mensagemErro.set(this.obterMensagemErro(response));
      },
    });
  }

  private obterMensagemErro(response: HttpErrorResponse): string {
    if (response.status === 409) {
      return 'Este e-mail já está cadastrado no sistema.';
    }

    if (response.status === 403) {
      return 'Seu perfil não tem permissão para cadastrar novos membros.';
    }

    const problem = response.error as ApiValidationProblemDetails | null;

    if (problem?.errors) {
      const mensagens = Object.values(problem.errors).flat();
      if (mensagens.length > 0) return mensagens.join('\n');
    }

    return problem?.detail ?? 'Não foi possível cadastrar o membro. Tente novamente.';
  }
}