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
const AUTH_API_URL = 'http://localhost:5200/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly sessaoSignal = signal<SessaoAutenticada | null>(this.lerSessaoSalva());

  public readonly sessao = this.sessaoSignal.asReadonly();
  public readonly token = computed(() => this.sessaoSignal()?.token ?? null);
  public readonly usuario = computed(() => this.sessaoSignal()?.usuario ?? null);
  public readonly estaAutenticado = computed(() => Boolean(this.token()));

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
          this.salvarSessao({ token: jwt });
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

      return JSON.parse(raw) as SessaoAutenticada;
    } catch {
      return null;
    }
  }

  private extrairRoleDoJwt(jwt: string): string | null {
    try {
      const partes = jwt.split('.');
      if (partes.length < 2) return null;

      const base64Url = partes[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const jsonPayload = atob(padded);
      const payload = JSON.parse(jsonPayload) as Record<string, unknown>;

      const role =
        payload['role'] ??
        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      if (typeof role === 'string') return role;
      if (Array.isArray(role) && typeof role[0] === 'string') return role[0];

      return null;
    } catch {
      return null;
    }
  }

  private ehPerfilValido(valor: string | undefined | null): valor is PerfilUsuario {
    return valor === 'Administrador' || valor === 'Gestor' || valor === 'Analista';
  }
}