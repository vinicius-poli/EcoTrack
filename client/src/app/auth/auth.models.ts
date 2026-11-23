export type PerfilUsuario = 'Administrador' | 'Gestor' | 'Analista';

export type PerfilMembroTenant = 'Gestor' | 'Analista';

export interface CadastroRequest {
  readonly nome: string;
  readonly email: string;
  readonly senha: string;
  readonly perfil?: string;
  readonly tenantId?: string | null;
}

export interface CadastrarMembroRequest {
  readonly nome: string;
  readonly email: string;
  readonly senha: string;
  readonly perfil: PerfilMembroTenant | '';
}

export interface CadastroConcluidoNavigationState {
  readonly emailCadastrado?: string;
}

export interface UsuarioDto {
  readonly id: string;
  readonly nome: string;
  readonly email: string;
  readonly perfil: PerfilUsuario | string;
  readonly tenantId: string;
}

export interface LoginRequest {
  readonly email: string;
  readonly senha: string;
}

export interface TokenDto {
  readonly token?: string;
  readonly accessToken?: string;
}

export interface SessaoAutenticada {
  readonly token: string;
  readonly usuario?: UsuarioDto;
}

export interface ApiValidationProblemDetails {
  readonly detail?: string;
  readonly errors?: Record<string, string[]>;
}