import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { STATUS_LABELS, WorkOrderStatus } from '../core/models';

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [attr.data-status]="status()"><span class="dot"></span>{{ label() }}</span>`,
  styles: `
    .badge {
      --tone: var(--muted);
      --tone-soft: #ece8df;
      display: inline-flex;
      align-items: center;
      gap: 7px;
      padding: 3px 10px 3px 9px;
      border-radius: 999px;
      background: var(--tone-soft);
      color: var(--tone);
      font-size: 12px;
      font-weight: 700;
      white-space: nowrap;
    }
    .dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: var(--tone);
    }
    .badge[data-status='Diagnosed'] {
      --tone: var(--info);
      --tone-soft: var(--info-soft);
    }
    .badge[data-status='Approved'] {
      --tone: var(--violet);
      --tone-soft: var(--violet-soft);
    }
    .badge[data-status='InProgress'] {
      --tone: var(--accent);
      --tone-soft: var(--accent-soft);
    }
    .badge[data-status='Completed'] {
      --tone: var(--success);
      --tone-soft: var(--success-soft);
    }
    .badge[data-status='Delivered'] {
      --tone: #2d6a64;
      --tone-soft: #dcefec;
    }
    .badge[data-status='Cancelled'] {
      --tone: var(--danger);
      --tone-soft: var(--danger-soft);
    }
  `
})
export class StatusBadgeComponent {
  readonly status = input.required<WorkOrderStatus>();
  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
}
