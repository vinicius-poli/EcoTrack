import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, EMPTY, finalize, Observable, tap } from 'rxjs';
import {
  CadastrarMembroRequest,
  CadastroRequest,
  LoginRequest,
  PerfilUsuario,
  SessaoAutenticada,
  TokenDto,
  UsuarioDto,
} from './auth.models';

const SESSAO_STORAGE_KEY = 'ecotrack_sessao_autenticada';
const AUTH_API_URL = 'https://ecotrack-webapi-febueegcctfsfgcq.westcentralus-01.azurewebsites.net/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessaoSignal = signal<SessaoAutenticada | null>(this.lerSessaoSalva());

  public readonly sessao = this.sessaoSignal.asReadonly();
  public readonly token = computed(() => this.sessaoSignal()?.token ?? null);
  public readonly usuario = computed(() => this.sessaoSignal()?.usuario ?? null);
  public readonly estaAutenticado = computed(() => Boolean(this.token()));

  public readonly nomeUsuario = computed<string | null>(() => {
    const sessaoAtual = this.sessaoSignal();
    if (!sessaoAtual?.token) return null;

    return (
      sessaoAtual.usuario?.nome ??
      this.extrairClaimDoJwt(sessaoAtual.token, [
        'name',
        'unique_name',
        'nome',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
      ])
    );
  });

  public readonly nomeOrganizacao = computed<string | null>(() => {
    const sessaoAtual = this.sessaoSignal();
    if (!sessaoAtual?.token) return null;

    return (
      sessaoAtual.usuario?.nomeOrganizacao ??
      this.extrairClaimDoJwt(sessaoAtual.token, [
        'nome_organizacao',
        'organizacao',
        'organization',
        'NomeOrganizacao',
      ])
    );
  });

  public readonly perfil = computed<PerfilUsuario | null>(() => {
    const sessaoAtual = this.sessaoSignal();
    if (!sessaoAtual?.token) return null;

    const perfilUsuario = sessaoAtual.usuario?.perfil;
    if (this.ehPerfilValido(perfilUsuario)) {
      return perfilUsuario;
    }

    const claimRole = this.extrairRoleDoJwt(sessaoAtual.token);
    return this.ehPerfilValido(claimRole) ? claimRole : null;
  });

  public readonly podeEscrever = computed(() => {
    const perfilAtual = this.perfil();
    return perfilAtual === 'Administrador' || perfilAtual === 'Gestor';
  });

  public readonly podeExcluir = computed(() => this.perfil() === 'Administrador');

  public readonly ehAdministrador = computed(() => this.perfil() === 'Administrador');

  public login(payload: LoginRequest): Observable<TokenDto> {
    return this.http.post<TokenDto>(`${AUTH_API_URL}/login`, payload).pipe(
      tap((resposta) => {
        const jwt = resposta.token ?? resposta.accessToken;
        if (jwt) {
          const usuarioFromClaims = this.montarUsuarioParcialDoJwt(jwt);
          this.salvarSessao({
            token: jwt,
            usuario: usuarioFromClaims ?? undefined,
          });
        }
      }),
    );
  }

  public cadastrar(payload: CadastroRequest): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(`${AUTH_API_URL}/registrar`, payload);
  }

  public cadastrarMembro(payload: CadastrarMembroRequest): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(`${AUTH_API_URL}/membros`, payload);
  }

  public logout(): Observable<void> {
    return this.http.post<void>(`${AUTH_API_URL}/logout`, {}).pipe(
      catchError(() => EMPTY),
      finalize(() => {
        this.limparSessaoLocal();
      }),
    );
  }

  public limparSessaoLocal(): void {
    this.sessaoSignal.set(null);

    try {
      localStorage.removeItem(SESSAO_STORAGE_KEY);
    } catch {
      // Ignora falhas de acesso ao storage em ambientes restritos
    }
  }

  private salvarSessao(sessao: SessaoAutenticada): void {
    this.sessaoSignal.set(sessao);

    try {
      localStorage.setItem(SESSAO_STORAGE_KEY, JSON.stringify(sessao));
    } catch {
      // Ignora falhas de acesso ao storage em ambientes restritos
    }
  }

  private lerSessaoSalva(): SessaoAutenticada | null {
    try {
      const raw = localStorage.getItem(SESSAO_STORAGE_KEY);
      if (!raw) return null;

      const sessao = JSON.parse(raw) as SessaoAutenticada;
      if (!sessao?.token) return null;

      return {
        ...sessao,
        usuario: sessao.usuario ?? this.montarUsuarioParcialDoJwt(sessao.token) ?? undefined,
      };
    } catch {
      return null;
    }
  }

  private montarUsuarioParcialDoJwt(jwt: string): UsuarioDto | null {
    const payload = this.decodificarPayloadJwt(jwt);
    if (!payload) return null;

    const id =
      this.lerClaimString(payload, [
        'sub',
        'nameid',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
      ]) ?? '';

    const tenantId =
      this.lerClaimString(payload, ['tenantId', 'tenant_id', 'TenantId']) ?? '';

    const nome =
      this.lerClaimString(payload, [
        'name',
        'unique_name',
        'nome',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
      ]) ?? '';

    const email =
      this.lerClaimString(payload, [
        'email',
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
      ]) ?? '';

    const roleClaim = this.extrairRoleDoJwt(jwt);
    const perfil: PerfilUsuario | string = this.ehPerfilValido(roleClaim)
      ? roleClaim
      : (roleClaim ?? '');

    const nomeOrganizacao = this.lerClaimString(payload, [
      'nome_organizacao',
      'organizacao',
      'organization',
      'NomeOrganizacao',
    ]);

    if (!id && !nome && !email && !nomeOrganizacao) {
      return null;
    }

    return {
      id,
      tenantId,
      nome,
      email,
      perfil,
      nomeOrganizacao,
    };
  }

  private extrairClaimDoJwt(jwt: string, chaves: string[]): string | null {
    const payload = this.decodificarPayloadJwt(jwt);
    if (!payload) return null;

    return this.lerClaimString(payload, chaves);
  }

  private lerClaimString(payload: Record<string, unknown>, chaves: string[]): string | null {
    for (const chave of chaves) {
      const valor = payload[chave];
      if (typeof valor === 'string' && valor.trim().length > 0) {
        return valor.trim();
      }
    }

    return null;
  }

  private decodificarPayloadJwt(jwt: string): Record<string, unknown> | null {
    try {
      const partes = jwt.split('.');
      if (partes.length < 2) return null;

      const base64Url = partes[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const jsonPayload = atob(padded);
      return JSON.parse(jsonPayload) as Record<string, unknown>;
    } catch {
      return null;
    }
  }

  private extrairRoleDoJwt(jwt: string): string | null {
    const payload = this.decodificarPayloadJwt(jwt);
    if (!payload) return null;

    const role =
      payload['role'] ??
      payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    if (typeof role === 'string') return role;
    if (Array.isArray(role) && typeof role[0] === 'string') return role[0];

    return null;
  }

  private ehPerfilValido(valor: string | undefined | null): valor is PerfilUsuario {
    return valor === 'Administrador' || valor === 'Gestor' || valor === 'Analista';
  }
}