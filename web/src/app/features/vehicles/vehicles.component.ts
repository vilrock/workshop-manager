import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Customer, Paged, Vehicle } from '../../core/models';
import { describeError } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { DataStateComponent } from '../../shared/data-state.component';
import { IconComponent } from '../../shared/icon.component';
import { ModalComponent } from '../../shared/modal.component';
import { PaginationComponent } from '../../shared/pagination.component';

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const CUSTOMER_PICKER_SIZE = 100;
const CURRENT_YEAR = new Date().getFullYear();

type VehicleField = 'customerId' | 'licensePlate' | 'make' | 'model' | 'year' | 'vin' | 'color' | 'mileageKm';

@Component({
  selector: 'app-vehicles',
  imports: [ReactiveFormsModule, DecimalPipe, DataStateComponent, IconComponent, ModalComponent, PaginationComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vehicles.component.html'
})
export class VehiclesComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly customerId = input<string>();

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly result = signal<Paged<Vehicle> | null>(null);
  protected readonly page = signal(1);

  protected readonly customers = signal<Customer[]>([]);
  protected readonly editing = signal<Vehicle | 'new' | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly filteredCustomerName = computed(() => {
    const items = this.result()?.items ?? [];
    return this.customerId() ? (items[0]?.customerName ?? 'selected customer') : null;
  });

  protected readonly form = this.formBuilder.nonNullable.group({
    customerId: ['', [Validators.required]],
    licensePlate: ['', [Validators.required, Validators.maxLength(12)]],
    make: ['', [Validators.required, Validators.maxLength(60)]],
    model: ['', [Validators.required, Validators.maxLength(60)]],
    year: [CURRENT_YEAR, [Validators.required, Validators.min(1950), Validators.max(CURRENT_YEAR + 1)]],
    vin: ['', [Validators.maxLength(17)]],
    color: ['', [Validators.maxLength(30)]],
    mileageKm: [0, [Validators.required, Validators.min(0), Validators.max(2000000)]]
  });

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    effect(() => {
      this.customerId();
      untracked(() => {
        this.page.set(1);
        this.load();
      });
    });
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.listVehicles(this.searchControl.value.trim(), this.customerId() ?? '', this.page(), PAGE_SIZE).subscribe({
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

  protected clearCustomerFilter(): void {
    void this.router.navigate(['/vehicles']);
  }

  protected openCreate(): void {
    this.form.reset({ customerId: this.customerId() ?? '', licensePlate: '', make: '', model: '', year: CURRENT_YEAR, vin: '', color: '', mileageKm: 0 });
    this.form.controls.customerId.enable();
    this.formError.set(null);
    this.loadCustomers();
    this.editing.set('new');
  }

  protected openEdit(vehicle: Vehicle): void {
    this.form.reset({
      customerId: vehicle.customerId,
      licensePlate: vehicle.licensePlate,
      make: vehicle.make,
      model: vehicle.model,
      year: vehicle.year,
      vin: vehicle.vin ?? '',
      color: vehicle.color ?? '',
      mileageKm: vehicle.mileageKm
    });
    this.form.controls.customerId.disable();
    this.formError.set(null);
    this.editing.set(vehicle);
  }

  protected closeForm(): void {
    this.editing.set(null);
  }

  protected isInvalid(name: VehicleField): boolean {
    const control = this.form.controls[name];
    return control.touched && control.invalid;
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const target = this.editing();
    const value = this.form.getRawValue();
    const payload = {
      licensePlate: value.licensePlate.trim(),
      make: value.make.trim(),
      model: value.model.trim(),
      year: Number(value.year),
      vin: value.vin.trim() || null,
      color: value.color.trim() || null,
      mileageKm: Number(value.mileageKm)
    };
    this.saving.set(true);
    this.formError.set(null);

    const request = target !== null && target !== 'new' ? this.api.updateVehicle(target.vehicleId, payload) : this.api.createVehicle(value.customerId, payload);

    request.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.editing.set(null);
        this.toast.success(target === 'new' ? 'Vehicle added' : 'Vehicle updated', `${saved.make} ${saved.model} (${saved.licensePlate})`);
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formError.set(describeError(error).detail);
      }
    });
  }

  private loadCustomers(): void {
    this.api.listCustomers('', 1, CUSTOMER_PICKER_SIZE).subscribe({
      next: (result) => this.customers.set(result.items.filter((customer) => customer.isActive)),
      error: (error: unknown) => this.formError.set(describeError(error).detail)
    });
  }
}
