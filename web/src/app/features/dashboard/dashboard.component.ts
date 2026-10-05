import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { DashboardStats, STATUS_LABELS, WorkOrderStatus } from '../../core/models';
import { describeError } from '../../core/problem';
import { DataStateComponent } from '../../shared/data-state.component';
import { IconComponent } from '../../shared/icon.component';
import { STATUS_COLORS } from '../../shared/status-colors';
import { StatusBadgeComponent } from '../../shared/status-badge.component';

interface StatusBar {
  status: WorkOrderStatus;
  label: string;
  count: number;
  share: number;
  color: string;
}

interface RevenueColumn {
  month: string;
  label: string;
  revenue: number;
  height: number;
  isCurrent: boolean;
}

const MONTH_FORMAT = new Intl.DateTimeFormat('en-US', { month: 'short', timeZone: 'UTC' });

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, CurrencyPipe, DatePipe, DecimalPipe, DataStateComponent, IconComponent, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  private readonly api = inject(ApiService);
  protected readonly auth = inject(AuthService);

  protected readonly loading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly stats = signal<DashboardStats | null>(null);

  protected readonly statusBars = computed<StatusBar[]>(() => {
    const entries = this.stats()?.ordersByStatus ?? [];
    const max = Math.max(1, ...entries.map((entry) => entry.count));
    return entries.map((entry) => ({
      status: entry.status,
      label: STATUS_LABELS[entry.status],
      count: entry.count,
      share: (entry.count / max) * 100,
      color: STATUS_COLORS[entry.status]
    }));
  });

  protected readonly revenueColumns = computed<RevenueColumn[]>(() => {
    const entries = this.stats()?.revenueByMonth ?? [];
    const max = Math.max(1, ...entries.map((entry) => entry.revenue));
    return entries.map((entry, index) => ({
      month: entry.month,
      label: MONTH_FORMAT.format(new Date(`${entry.month}-01T00:00:00Z`)),
      revenue: entry.revenue,
      height: Math.max(entry.revenue > 0 ? 4 : 0, (entry.revenue / max) * 100),
      isCurrent: index === entries.length - 1
    }));
  });

  protected readonly firstName = computed(() => this.auth.user()?.fullName.split(' ')[0] ?? '');

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.dashboardStats().subscribe({
      next: (stats) => {
        this.stats.set(stats);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(describeError(error).detail);
        this.loading.set(false);
      }
    });
  }
}
