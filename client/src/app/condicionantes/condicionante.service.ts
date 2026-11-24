import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AtualizarCondicionanteRequest,
  CondicionanteDto,
  CriarCondicionanteRequest,
} from './condicionante.models';

@Injectable({ providedIn: 'root' })
export class CondicionanteService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5200/api/Condicionantes';

  selecionarTodos(licencaId?: string | null): Observable<CondicionanteDto[]> {
    let params = new HttpParams();
    if (licencaId) {
      params = params.set('licencaId', licencaId);
    }

    return this.http.get<CondicionanteDto[]>(this.apiUrl, { params });
  }

  selecionarPorId(id: string): Observable<CondicionanteDto> {
    return this.http.get<CondicionanteDto>(`${this.apiUrl}/${id}`);
  }

  cadastrar(registro: CriarCondicionanteRequest): Observable<CondicionanteDto> {
    return this.http.post<CondicionanteDto>(this.apiUrl, registro);
  }

  atualizar(id: string, registro: AtualizarCondicionanteRequest): Observable<CondicionanteDto> {
    return this.http.put<CondicionanteDto>(`${this.apiUrl}/${id}`, registro);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  adicionarEvidencia(
    condicionanteId: string,
    arquivo: File,
    descricao?: string | null,
  ): Observable<CondicionanteDto | void> {
    const formData = new FormData();
    formData.append('arquivo', arquivo, arquivo.name);

    if (descricao?.trim()) {
      formData.append('descricao', descricao.trim());
    }

    return this.http.post<CondicionanteDto | void>(
      `${this.apiUrl}/${condicionanteId}/evidencias`,
      formData,
    );
  }

  baixarEvidencia(condicionanteId: string, evidenciaId: string): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/${condicionanteId}/evidencias/${evidenciaId}/download`,
      { responseType: 'blob' },
    );
  }

  excluirEvidencia(condicionanteId: string, evidenciaId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.apiUrl}/${condicionanteId}/evidencias/${evidenciaId}`,
    );
  }
}