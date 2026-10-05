import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, inject, input, signal, untracked } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { HistoryEntry, LineItemType, Mechanic, STATUS_LABELS, TRANSITION_ACTION_LABELS, WorkOrderDetail, WorkOrderStatus } from '../../core/models';
import { describeError } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { DataStateComponent } from '../../shared/data-state.component';
import { IconComponent } from '../../shared/icon.component';
import { ModalComponent } from '../../shared/modal.component';
import { StatusBadgeComponent } from '../../shared/status-badge.component';

@Component({
  selector: 'app-work-order-detail',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe, DatePipe, DecimalPipe, DataStateComponent, IconComponent, ModalComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './work-order-detail.component.html',
  styleUrl: './work-order-detail.component.scss'
})
export class WorkOrderDetailComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);

  readonly workOrderId = input.required<string>();

  protected readonly statusLabels = STATUS_LABELS;
  protected readonly actionLabels = TRANSITION_ACTION_LABELS;

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<{ title: string; detail: string } | null>(null);
  protected readonly order = signal<WorkOrderDetail | null>(null);
  protected readonly mechanics = signal<Mechanic[]>([]);

  protected readonly pendingTransition = signal<WorkOrderStatus | null>(null);
  protected readonly working = signal(false);
  protected readonly noteControl = new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] });
  protected readonly mechanicControl = new FormControl('', { nonNullable: true, validators: [Validators.required] });

  protected readonly itemForm = this.formBuilder.nonNullable.group({
    itemType: ['Part' as LineItemType, [Validators.required]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    quantity: [1, [Validators.required, Validators.min(0.01), Validators.max(1000)]],
    unitPrice: [0, [Validators.required, Validators.min(0), Validators.max(1000000)]]
  });

  constructor() {
    effect(() => {
      this.workOrderId();
      untracked(() => this.load());
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.getWorkOrder(this.workOrderId()).subscribe({
      next: (order) => {
        this.applyOrder(order);
        this.loading.set(false);
        this.loadMechanicsIfNeeded(order);
      },
      error: (error: unknown) => {
        const described = describeError(error);
        this.errorMessage.set({ title: described.status === 404 ? 'Work order not found' : described.status === 403 ? 'You cannot open this work order' : 'The work order could not load', detail: described.detail });
        this.loading.set(false);
      }
    });
  }

  protected historyLabel(entry: HistoryEntry): string {
    if (entry.fromStatus === null) {
      return 'Order created';
    }
    return entry.fromStatus === entry.toStatus ? 'Mechanic assigned' : `${STATUS_LABELS[entry.fromStatus]} to ${STATUS_LABELS[entry.toStatus]}`;
  }

  protected isForward(status: WorkOrderStatus): boolean {
    return status !== 'Cancelled';
  }

  protected openTransition(status: WorkOrderStatus): void {
    this.noteControl.reset('');
    this.pendingTransition.set(status);
  }

  protected closeTransition(): void {
    this.pendingTransition.set(null);
  }

  protected confirmTransition(): void {
    const target = this.pendingTransition();
    const current = this.order();
    if (target === null || current === null || this.noteControl.invalid) {
      return;
    }

    this.working.set(true);
    this.api.transitionWorkOrder(current.workOrderId, target, current.status, this.noteControl.value.trim() || null).subscribe({
      next: (updated) => {
        this.applyOrder(updated);
        this.working.set(false);
        this.pendingTransition.set(null);
        this.toast.success(`${updated.orderNumber} is now ${STATUS_LABELS[updated.status].toLowerCase()}`);
      },
      error: (error: unknown) => this.handleMutationError(error, () => this.pendingTransition.set(null))
    });
  }

  protected assignMechanic(): void {
    const current = this.order();
    if (current === null || this.mechanicControl.invalid) {
      this.mechanicControl.markAsTouched();
      return;
    }

    this.working.set(true);
    this.api.assignMechanic(current.workOrderId, this.mechanicControl.value).subscribe({
      next: (updated) => {
        this.applyOrder(updated);
        this.working.set(false);
        this.toast.success('Mechanic assigned', updated.assignedMechanicName ?? undefined);
      },
      error: (error: unknown) => this.handleMutationError(error)
    });
  }

  protected addItem(): void {
    const current = this.order();
    if (current === null) {
      return;
    }

    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const value = this.itemForm.getRawValue();
    this.working.set(true);
    this.api
      .addLineItem(current.workOrderId, { itemType: value.itemType, description: value.description.trim(), quantity: Number(value.quantity), unitPrice: Number(value.unitPrice) })
      .subscribe({
        next: (updated) => {
          this.applyOrder(updated);
          this.working.set(false);
          this.itemForm.reset({ itemType: value.itemType, description: '', quantity: 1, unitPrice: 0 });
          this.toast.success('Line added', `New total ${new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(updated.totalAmount)}`);
        },
        error: (error: unknown) => this.handleMutationError(error)
      });
  }

  protected isItemFieldInvalid(name: 'description' | 'quantity' | 'unitPrice'): boolean {
    const control = this.itemForm.controls[name];
    return control.touched && control.invalid;
  }

  private applyOrder(order: WorkOrderDetail): void {
    this.order.set(order);
    this.mechanicControl.setValue(order.assignedMechanicId ?? '');
  }

  private loadMechanicsIfNeeded(order: WorkOrderDetail): void {
    if (!order.canAssignMechanic || this.mechanics().length > 0) {
      return;
    }
    this.api.listMechanics().subscribe({ next: (mechanics) => this.mechanics.set(mechanics) });
  }

  private handleMutationError(error: unknown, onFinished?: () => void): void {
    const described = describeError(error);
    this.working.set(false);
    onFinished?.();

    if (described.status === 409) {
      this.toast.info('This order was changed by someone else', 'The latest version has been loaded.');
      this.load();
      return;
    }

    this.toast.error(described.title, Object.values(described.fieldErrors)[0]?.[0] ?? described.detail);
  }
}
