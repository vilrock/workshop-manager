import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Mechanic, Paged, STATUS_LABELS, TRANSITION_ACTION_LABELS, WORK_ORDER_STATUSES, WorkOrderDetail, WorkOrderStatus, WorkOrderSummary } from '../../core/models';
import { describeError } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { DataStateComponent } from '../../shared/data-state.component';
import { IconComponent } from '../../shared/icon.component';
import { PaginationComponent } from '../../shared/pagination.component';
import { STATUS_COLORS } from '../../shared/status-colors';
import { StatusBadgeComponent } from '../../shared/status-badge.component';
import { NewWorkOrderDialogComponent } from './new-work-order-dialog.component';

const LIST_PAGE_SIZE = 10;
const BOARD_PAGE_SIZE = 100;
const SEARCH_DEBOUNCE_MS = 300;

type ViewMode = 'list' | 'board';

interface BoardColumn {
  status: WorkOrderStatus;
  label: string;
  color: string;
  orders: WorkOrderSummary[];
}

@Component({
  selector: 'app-work-orders',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe, DataStateComponent, IconComponent, PaginationComponent, StatusBadgeComponent, NewWorkOrderDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './work-orders.component.html',
  styleUrl: './work-orders.component.scss'
})
export class WorkOrdersComponent {
  private readonly api = inject(ApiService);
  protected readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly auth = inject(AuthService);

  readonly view = input<string>();
  readonly createFlag = input<string>(undefined, { alias: 'new' });

  protected readonly statuses = WORK_ORDER_STATUSES;
  protected readonly statusLabels = STATUS_LABELS;
  protected readonly actionLabels = TRANSITION_ACTION_LABELS;
  protected readonly pageSize = LIST_PAGE_SIZE;

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly statusControl = new FormControl<WorkOrderStatus | ''>('', { nonNullable: true });
  protected readonly mechanicControl = new FormControl('', { nonNullable: true });

  protected readonly mode = computed<ViewMode>(() => (this.view() === 'board' ? 'board' : 'list'));
  protected readonly creating = computed(() => this.createFlag() !== undefined && this.auth.isStaffFrontDesk());

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly result = signal<Paged<WorkOrderSummary> | null>(null);
  protected readonly page = signal(1);
  protected readonly mechanics = signal<Mechanic[]>([]);
  protected readonly advancingId = signal<string | null>(null);

  protected readonly columns = computed<BoardColumn[]>(() => {
    const orders = this.result()?.items ?? [];
    return WORK_ORDER_STATUSES.map((status) => ({
      status,
      label: STATUS_LABELS[status],
      color: STATUS_COLORS[status],
      orders: orders.filter((order) => order.status === status)
    }));
  });

  constructor() {
    if (this.auth.isStaffFrontDesk()) {
      this.api.listMechanics().subscribe({ next: (mechanics) => this.mechanics.set(mechanics) });
    }

    this.searchControl.valueChanges.pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());
    this.statusControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());
    this.mechanicControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.reload());

    effect(() => {
      this.mode();
      untracked(() => this.reload());
    });
  }

  protected reload(): void {
    this.page.set(1);
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    const board = this.mode() === 'board';

    this.api
      .listWorkOrders({
        search: this.searchControl.value.trim(),
        mechanicId: this.mechanicControl.value,
        status: board ? '' : this.statusControl.value,
        page: board ? 1 : this.page(),
        pageSize: board ? BOARD_PAGE_SIZE : LIST_PAGE_SIZE
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.errorMessage.set(describeError(error).detail);
          this.loading.set(false);
        }
      });
  }

  protected changePage(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected setMode(mode: ViewMode): void {
    void this.router.navigate([], { queryParams: { view: mode }, queryParamsHandling: 'merge' });
  }

  protected openCreate(): void {
    void this.router.navigate([], { queryParams: { new: 1 }, queryParamsHandling: 'merge' });
  }

  protected closeCreate(): void {
    void this.router.navigate([], { queryParams: { new: null }, queryParamsHandling: 'merge' });
  }

  protected onCreated(detail: WorkOrderDetail): void {
    this.toast.success('Work order created', `${detail.orderNumber} for ${detail.customerName}`);
    void this.router.navigate(['/work-orders', detail.workOrderId]);
  }

  protected forwardTransitions(order: WorkOrderSummary): WorkOrderStatus[] {
    return order.allowedTransitions.filter((status) => status !== 'Cancelled');
  }

  protected advance(order: WorkOrderSummary, target: WorkOrderStatus): void {
    this.advancingId.set(order.workOrderId);
    this.api.transitionWorkOrder(order.workOrderId, target, order.status, null).subscribe({
      next: (detail) => {
        this.replaceOrder(detail);
        this.advancingId.set(null);
        this.toast.success(`${order.orderNumber} moved to ${STATUS_LABELS[target]}`);
      },
      error: (error: unknown) => {
        const described = describeError(error);
        this.advancingId.set(null);
        if (described.status === 409) {
          this.toast.info('Order changed elsewhere', 'The board was refreshed with the latest status.');
          this.load();
          return;
        }
        this.toast.error(described.title, described.detail);
      }
    });
  }

  private replaceOrder(updated: WorkOrderSummary): void {
    this.result.update((current) => (current ? { ...current, items: current.items.map((order) => (order.workOrderId === updated.workOrderId ? updated : order)) } : current));
  }
}
