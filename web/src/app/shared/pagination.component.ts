import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-pagination',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (totalCount() > 0) {
      <nav class="pagination" aria-label="Pagination">
        <span class="summary num">{{ rangeStart() }}-{{ rangeEnd() }} of {{ totalCount() }}</span>
        <div class="controls">
          <button class="btn btn-sm btn-icon" type="button" aria-label="Previous page" [disabled]="page() <= 1" (click)="pageChange.emit(page() - 1)">
            <app-icon name="chevron-left" [size]="16" />
          </button>
          <span class="current num">Page {{ page() }} of {{ totalPages() }}</span>
          <button class="btn btn-sm btn-icon" type="button" aria-label="Next page" [disabled]="page() >= totalPages()" (click)="pageChange.emit(page() + 1)">
            <app-icon name="chevron-right" [size]="16" />
          </button>
        </div>
      </nav>
    }
  `,
  styles: `
    .pagination {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: 10px;
      padding: 12px 16px;
      border-top: 1px solid var(--line);
      background: var(--surface-2);
      border-radius: 0 0 var(--radius) var(--radius);
    }
    .summary,
    .current {
      font-size: 12.5px;
      color: var(--muted);
      font-weight: 600;
    }
    .controls {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .btn-icon {
      width: 30px;
    }
  `
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly rangeStart = computed(() => (this.page() - 1) * this.pageSize() + 1);
  protected readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));
}
