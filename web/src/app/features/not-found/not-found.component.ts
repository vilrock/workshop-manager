import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="wrap">
      <span class="code mono">404</span>
      <h1>This page is not on the lift</h1>
      <p>The address you opened does not exist or you no longer have access to it.</p>
      <a class="btn btn-primary" routerLink="/dashboard">Back to dashboard</a>
    </main>
  `,
  styles: `
    .wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 12px;
      min-height: 100dvh;
      padding: 24px;
      text-align: center;
    }
    .code {
      padding: 4px 12px;
      border-radius: 6px;
      background: var(--accent-soft);
      color: var(--accent-ink);
      font-size: 14px;
    }
    p {
      max-width: 380px;
      margin-bottom: 8px;
      color: var(--muted);
    }
  `
})
export class NotFoundComponent {}
