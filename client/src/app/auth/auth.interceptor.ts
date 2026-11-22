import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

const ROTAS_PUBLICAS_AUTH = ['/api/auth/login', '/api/auth/registrar'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.token();

  const ehRotaPublicaAuth = ROTAS_PUBLICAS_AUTH.some((rota) => req.url.includes(rota));

  if (!token || ehRotaPublicaAuth) {
    return next(req);
  }

  const requisicaoAutenticada = req.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(requisicaoAutenticada);
};