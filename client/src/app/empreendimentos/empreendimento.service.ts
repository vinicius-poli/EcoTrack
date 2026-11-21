import { HttpClient } from '@angular/common/http';
import { inject, Service } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CriarEmpreendimentoRequest,
  EmpreendimentoDto,
} from './empreendimento.models';

@Service()
export class EmpreendimentoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5200/api/empreendimentos'; // Ajustaremos via InjectionToken depois

  selecionarTodos(): Observable<EmpreendimentoDto[]> {
    return this.http.get<EmpreendimentoDto[]>(this.apiUrl);
  }

  cadastrar(registro: CriarEmpreendimentoRequest): Observable<EmpreendimentoDto> {
    return this.http.post<EmpreendimentoDto>(this.apiUrl, registro);
  }
}