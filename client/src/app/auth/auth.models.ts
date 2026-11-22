export interface CadastroRequest {
  readonly nome: string;
  readonly email: string;
  readonly senha: string;
}

export interface CadastroConcluidoNavigationState {
  readonly emailCadastrado?: string;
}

export interface UsuarioDto {
  readonly id: string;
  readonly nome: string;
  readonly email: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly senha: string;
}

export interface TokenDto {
  readonly token: string;
}

export interface SessaoAutenticada {
  readonly token: string;
  readonly usuario?: UsuarioDto;
}

export interface ApiValidationProblemDetails {
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}