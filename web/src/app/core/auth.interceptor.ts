import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const API_PREFIX = '/api/';
const LOGIN_PATH = '/auth/login';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.startsWith(API_PREFIX)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const token = auth.accessToken;
  const headers: Record<string, string> = { 'X-Correlation-Id': crypto.randomUUID() };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return next(request.clone({ setHeaders: headers })).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !request.url.endsWith(LOGIN_PATH)) {
        auth.logout();
      }
      return throwError(() => error);
    })
  );
};
