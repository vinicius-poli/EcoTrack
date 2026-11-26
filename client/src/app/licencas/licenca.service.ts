import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AtualizarLicencaRequest,
  CriarLicencaRequest,
  LicencaDto,
} from './licenca.models';

@Injectable({ providedIn: 'root' })
export class LicencaService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://ecotrack-webapi-febueegcctfsfgcq.westcentralus-01.azurewebsites.net/api/licencas'; // Ajustaremos via InjectionToken depois

  selecionarTodos(empreendimentoId?: string | null): Observable<LicencaDto[]> {
    let params = new HttpParams();
    if (empreendimentoId) {
      params = params.set('empreendimentoId', empreendimentoId);
    }

    return this.http.get<LicencaDto[]>(this.apiUrl, { params });
  }

  selecionarPorId(id: string): Observable<LicencaDto> {
    return this.http.get<LicencaDto>(`${this.apiUrl}/${id}`);
  }

  cadastrar(registro: CriarLicencaRequest): Observable<LicencaDto> {
    return this.http.post<LicencaDto>(this.apiUrl, registro);
  }

  atualizar(id: string, registro: AtualizarLicencaRequest): Observable<LicencaDto> {
    return this.http.put<LicencaDto>(`${this.apiUrl}/${id}`, registro);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}