import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { UserRole } from './models';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? true : inject(Router).createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.isAuthenticated() ? inject(Router).createUrlTree(['/dashboard']) : true;
};

export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const allowed = (route.data['roles'] ?? []) as UserRole[];
  return auth.hasRole(allowed) ? true : inject(Router).createUrlTree(['/dashboard']);
};
