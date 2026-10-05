import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  title: string;
  detail?: string;
}

const AUTO_DISMISS_MS = 5500;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  readonly toasts = signal<Toast[]>([]);

  success(title: string, detail?: string): void {
    this.push('success', title, detail);
  }

  error(title: string, detail?: string): void {
    this.push('error', title, detail);
  }

  info(title: string, detail?: string): void {
    this.push('info', title, detail);
  }

  dismiss(id: number): void {
    this.toasts.update((current) => current.filter((toast) => toast.id !== id));
  }

  private push(kind: ToastKind, title: string, detail?: string): void {
    const id = this.nextId++;
    this.toasts.update((current) => [...current, { id, kind, title, detail }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }
}
