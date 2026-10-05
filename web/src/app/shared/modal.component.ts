import { ChangeDetectionStrategy, Component, ElementRef, effect, input, output, viewChild } from '@angular/core';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-modal',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'closed.emit()' },
  template: `
    <div class="backdrop" (click)="closed.emit()"></div>
    <div #dialog class="dialog" role="dialog" aria-modal="true" [attr.aria-label]="heading()" tabindex="-1" [style.max-width.px]="width()">
      <header class="dialog-header">
        <h2>{{ heading() }}</h2>
        <button class="btn btn-ghost btn-icon btn-sm" type="button" aria-label="Close dialog" (click)="closed.emit()">
          <app-icon name="x" />
        </button>
      </header>
      <div class="dialog-body">
        <ng-content />
      </div>
    </div>
  `,
  styles: `
    :host {
      position: fixed;
      inset: 0;
      z-index: 60;
      display: grid;
      place-items: center;
      padding: 16px;
    }
    .backdrop {
      position: absolute;
      inset: 0;
      background: rgba(28, 27, 25, 0.55);
      animation: fade 0.15s ease-out both;
    }
    .dialog {
      position: relative;
      width: 100%;
      max-height: calc(100dvh - 32px);
      overflow-y: auto;
      border-radius: 16px;
      background: var(--surface);
      box-shadow: var(--shadow-md);
      animation: rise 0.2s ease-out both;
    }
    .dialog:focus {
      outline: none;
    }
    .dialog-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 18px 20px 14px;
      border-bottom: 1px solid var(--line);
    }
    .dialog-body {
      padding: 20px;
    }
    .btn-icon {
      width: 30px;
    }
    @keyframes fade {
      from {
        opacity: 0;
      }
    }
    @keyframes rise {
      from {
        opacity: 0;
        transform: translateY(12px) scale(0.98);
      }
    }
  `
})
export class ModalComponent {
  readonly heading = input.required<string>();
  readonly width = input(560);
  readonly closed = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLElement>>('dialog');

  constructor() {
    effect(() => this.dialog().nativeElement.focus());
  }
}
