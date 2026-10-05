import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../core/toast.service';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-toast-container',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="stack" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [attr.data-kind]="toast.kind" role="status">
          <span class="mark"><app-icon [name]="toast.kind === 'success' ? 'check' : 'alert'" [size]="16" /></span>
          <div class="text">
            <strong>{{ toast.title }}</strong>
            @if (toast.detail) {
              <span>{{ toast.detail }}</span>
            }
          </div>
          <button type="button" class="close" aria-label="Dismiss notification" (click)="toasts.dismiss(toast.id)">
            <app-icon name="x" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .stack {
      position: fixed;
      right: 16px;
      bottom: 16px;
      z-index: 80;
      display: flex;
      flex-direction: column;
      gap: 10px;
      width: min(380px, calc(100vw - 32px));
    }
    .toast {
      --tone: var(--info);
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 12px 12px 14px;
      border-radius: 12px;
      border-left: 4px solid var(--tone);
      background: var(--graphite);
      color: #f3f0ea;
      box-shadow: var(--shadow-md);
      animation: slide 0.22s ease-out both;
    }
    .toast[data-kind='success'] {
      --tone: #6fbf84;
    }
    .toast[data-kind='error'] {
      --tone: #ef7b6e;
    }
    .mark {
      display: grid;
      place-items: center;
      width: 24px;
      height: 24px;
      border-radius: 50%;
      color: var(--tone);
      background: rgba(255, 255, 255, 0.08);
      flex: none;
    }
    .text {
      display: flex;
      flex-direction: column;
      flex: 1;
      gap: 2px;
      font-size: 13px;
    }
    .text span {
      color: var(--sidebar-text);
    }
    .close {
      border: none;
      background: transparent;
      color: var(--sidebar-text);
      padding: 4px;
      border-radius: 6px;
    }
    .close:hover {
      background: rgba(255, 255, 255, 0.1);
    }
    @keyframes slide {
      from {
        opacity: 0;
        transform: translateX(20px);
      }
    }
  `
})
export class ToastContainerComponent {
  protected readonly toasts = inject(ToastService);
}
