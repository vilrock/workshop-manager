import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Customer, Paged } from '../../core/models';
import { describeError } from '../../core/problem';
import { ToastService } from '../../core/toast.service';
import { DataStateComponent } from '../../shared/data-state.component';
import { IconComponent } from '../../shared/icon.component';
import { ModalComponent } from '../../shared/modal.component';
import { PaginationComponent } from '../../shared/pagination.component';

const PAGE_SIZE = 10;
const SEARCH_DEBOUNCE_MS = 300;
const PHONE_PATTERN = /^[-0-9+() ]{7,25}$/;

@Component({
  selector: 'app-customers',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, DataStateComponent, IconComponent, ModalComponent, PaginationComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './customers.component.html'
})
export class CustomersComponent {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly pageSize = PAGE_SIZE;

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly result = signal<Paged<Customer> | null>(null);
  protected readonly page = signal(1);

  protected readonly editing = signal<Customer | 'new' | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    fullName: ['', [Validators.required, Validators.maxLength(120)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    phone: ['', [Validators.required, Validators.pattern(PHONE_PATTERN)]],
    address: ['', [Validators.maxLength(250)]],
    isActive: [true]
  });

  constructor() {
    this.searchControl.valueChanges
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.listCustomers(this.searchControl.value.trim(), this.page(), PAGE_SIZE).subscribe({
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

  protected openCreate(): void {
    this.form.reset({ fullName: '', email: '', phone: '', address: '', isActive: true });
    this.formError.set(null);
    this.editing.set('new');
  }

  protected openEdit(customer: Customer): void {
    this.form.reset({
      fullName: customer.fullName,
      email: customer.email,
      phone: customer.phone,
      address: customer.address ?? '',
      isActive: customer.isActive
    });
    this.formError.set(null);
    this.editing.set(customer);
  }

  protected closeForm(): void {
    this.editing.set(null);
  }

  protected isInvalid(name: 'fullName' | 'email' | 'phone' | 'address'): boolean {
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
    const payload = { fullName: value.fullName, email: value.email, phone: value.phone, address: value.address.trim() || null };
    this.saving.set(true);
    this.formError.set(null);

    const request = target !== null && target !== 'new' ? this.api.updateCustomer(target.customerId, { ...payload, isActive: value.isActive }) : this.api.createCustomer(payload);

    request.subscribe({
      next: (saved) => {
        this.saving.set(false);
        this.editing.set(null);
        this.toast.success(target === 'new' ? 'Customer created' : 'Customer updated', saved.fullName);
        this.load();
      },
      error: (error: unknown) => {
        this.saving.set(false);
        this.formError.set(describeError(error).detail);
      }
    });
  }
}
