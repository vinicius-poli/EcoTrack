import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AtualizarEmpreendimentoRequest,
  CriarEmpreendimentoRequest,
  EmpreendimentoDto,
} from './empreendimento.models';

@Injectable({ providedIn: 'root' })
export class EmpreendimentoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5200/api/empreendimentos'; // Ajustaremos via InjectionToken depois

  selecionarTodos(): Observable<EmpreendimentoDto[]> {
    return this.http.get<EmpreendimentoDto[]>(this.apiUrl);
  }

  selecionarPorId(id: string): Observable<EmpreendimentoDto> {
    return this.http.get<EmpreendimentoDto>(`${this.apiUrl}/${id}`);
  }

  cadastrar(registro: CriarEmpreendimentoRequest): Observable<EmpreendimentoDto> {
    return this.http.post<EmpreendimentoDto>(this.apiUrl, registro);
  }

  atualizar(id: string, registro: AtualizarEmpreendimentoRequest): Observable<EmpreendimentoDto> {
    return this.http.put<EmpreendimentoDto>(`${this.apiUrl}/${id}`, registro);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}