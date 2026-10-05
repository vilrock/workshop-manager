import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { LoginResponse, User, UserRole } from './models';

const STORAGE_KEY = 'workshop.session';

interface Session {
  accessToken: string;
  expiresAtUtc: string;
  user: User;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly session = signal<Session | null>(this.readStoredSession());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly role = computed<UserRole | null>(() => this.user()?.role ?? null);
  readonly isStaffFrontDesk = computed(() => this.role() === 'Admin' || this.role() === 'Advisor');

  get accessToken(): string | null {
    return this.isAuthenticated() ? this.session()!.accessToken : null;
  }

  isAuthenticated(): boolean {
    const current = this.session();
    return current !== null && new Date(current.expiresAtUtc).getTime() > Date.now();
  }

  hasRole(roles: readonly UserRole[]): boolean {
    const role = this.role();
    return role !== null && roles.includes(role);
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.api.login(email, password).pipe(
      tap((response) => this.storeSession({ accessToken: response.accessToken, expiresAtUtc: response.expiresAtUtc, user: response.user }))
    );
  }

  logout(): void {
    this.session.set(null);
    this.clearStoredSession();
    void this.router.navigate(['/login']);
  }

  private storeSession(session: Session): void {
    this.session.set(session);
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch {
      return;
    }
  }

  private clearStoredSession(): void {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      return;
    }
  }

  private readStoredSession(): Session | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as Session) : null;
    } catch {
      return null;
    }
  }
}
