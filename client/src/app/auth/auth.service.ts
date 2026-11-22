import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  CadastroRequest,
  LoginRequest,
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

  public login(payload: LoginRequest): Observable<TokenDto> {
    return this.http.post<TokenDto>(`${AUTH_API_URL}/login`, payload).pipe(
      tap((resposta) => {
        this.salvarSessao({ token: resposta.token });
      }),
    );
  }

  public registrar(payload: CadastroRequest): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(`${AUTH_API_URL}/registrar`, payload);
  }

  public logout(): void {
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
}