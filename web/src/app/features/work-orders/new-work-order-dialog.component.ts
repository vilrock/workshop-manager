import { ChangeDetectionStrategy, Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Customer, Vehicle, WorkOrderDetail } from '../../core/models';
import { describeError } from '../../core/problem';
import { ModalComponent } from '../../shared/modal.component';

const CUSTOMER_PICKER_SIZE = 100;

@Component({
  selector: 'app-new-work-order-dialog',
  imports: [ReactiveFormsModule, ModalComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-modal heading="New work order" (closed)="closed.emit()">
      <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <div class="form-grid">
          <div class="field full">
            <label for="order-customer">Customer</label>
            <select id="order-customer" class="select" formControlName="customerId" [class.invalid]="isInvalid('customerId')">
              <option value="">Select a customer</option>
              @for (customer of customers(); track customer.customerId) {
                <option [value]="customer.customerId">{{ customer.fullName }}</option>
              }
            </select>
            @if (isInvalid('customerId')) {
              <span class="field-error">Choose the customer.</span>
            }
          </div>
          <div class="field full">
            <label for="order-vehicle">Vehicle</label>
            <select id="order-vehicle" class="select" formControlName="vehicleId" [class.invalid]="isInvalid('vehicleId')">
              <option value="">{{ form.controls.customerId.value ? (vehicles().length ? 'Select a vehicle' : 'This customer has no vehicles yet') : 'Select a customer first' }}</option>
              @for (vehicle of vehicles(); track vehicle.vehicleId) {
                <option [value]="vehicle.vehicleId">{{ vehicle.year }} {{ vehicle.make }} {{ vehicle.model }} ({{ vehicle.licensePlate }})</option>
              }
            </select>
            @if (isInvalid('vehicleId')) {
              <span class="field-error">Choose the vehicle.</span>
            }
          </div>
          <div class="field full">
            <label for="order-description">What does the customer report?</label>
            <textarea id="order-description" class="textarea" formControlName="description" [class.invalid]="isInvalid('description')"></textarea>
            @if (isInvalid('description')) {
              <span class="field-error">Describe the problem in 3 to 500 characters.</span>
            }
          </div>
          <div class="field">
            <label for="order-mileage">Mileage at check-in (km)</label>
            <input id="order-mileage" class="input" type="number" formControlName="mileageKm" [class.invalid]="isInvalid('mileageKm')" />
            @if (isInvalid('mileageKm')) {
              <span class="field-error">Enter a mileage from 0 to 2,000,000.</span>
            }
          </div>
        </div>

        @if (formError(); as message) {
          <p class="field-error" role="alert" style="margin-top: 14px">{{ message }}</p>
        }

        <div class="toolbar" style="justify-content: flex-end; margin-top: 20px">
          <button class="btn" type="button" (click)="closed.emit()">Cancel</button>
          <button class="btn btn-primary" type="submit" [disabled]="saving()">{{ saving() ? 'Creating...' : 'Create work order' }}</button>
        </div>
      </form>
    </app-modal>
  `
})
export class NewWorkOrderDialogComponent {
  private readonly api = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly created = output<WorkOrderDetail>();
  readonly closed = output<void>();

  protected readonly customers = signal<Customer[]>([]);
  protected readonly vehicles = signal<Vehicle[]>([]);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    customerId: ['', [Validators.required]],
    vehicleId: ['', [Validators.required]],
    description: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(500)]],
    mileageKm: [0, [Validators.required, Validators.min(0), Validators.max(2000000)]]
  });

  private idempotencyKey = crypto.randomUUID();

  constructor() {
    this.api.listCustomers('', 1, CUSTOMER_PICKER_SIZE).subscribe({
      next: (result) => this.customers.set(result.items.filter((customer) => customer.isActive)),
      error: (error: unknown) => this.formError.set(describeError(error).detail)
    });

    this.form.controls.customerId.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((customerId) => this.loadVehicles(customerId));
    this.form.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.idempotencyKey = crypto.randomUUID();
    });
  }

  protected isInvalid(name: 'customerId' | 'vehicleId' | 'description' | 'mileageKm'): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.formError.set(null);

    this.api.createWorkOrder({ ...value, description: value.description.trim(), mileageKm: Number(value.mileageKm) }, this.idempotencyKey).subscribe({
      next: (detail) => {
        this.saving.set(false);
        this.created.emit(detail);
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formError.set(describeError(error).detail);
      }
    });
  }

  private loadVehicles(customerId: string): void {
    this.vehicles.set([]);
    this.form.controls.vehicleId.setValue('', { emitEvent: false });
    if (!customerId) {
      return;
    }

    this.api.listCustomerVehicles(customerId).subscribe({
      next: (vehicles) => this.vehicles.set(vehicles),
      error: (error: unknown) => this.formError.set(describeError(error).detail)
    });
  }
}
