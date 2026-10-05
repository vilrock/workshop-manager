import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IconComponent } from './icon.component';

@Component({
  selector: 'app-data-state',
  imports: [IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (kind() === 'error') {
      <div class="state error" role="alert">
        <span class="state-icon"><app-icon name="alert" [size]="22" /></span>
        <h3>{{ heading() }}</h3>
        <p>{{ message() }}</p>
        <button class="btn btn-sm" type="button" (click)="retry.emit()"><app-icon name="refresh" [size]="14" /> Try again</button>
      </div>
    } @else {
      <div class="state">
        <span class="state-icon"><app-icon [name]="icon()" [size]="22" /></span>
        <h3>{{ heading() }}</h3>
        <p>{{ message() }}</p>
        <ng-content />
      </div>
    }
  `
})
export class DataStateComponent {
  readonly kind = input<'empty' | 'error'>('empty');
  readonly heading = input.required<string>();
  readonly message = input('');
  readonly icon = input('inbox');
  readonly retry = output<void>();
}
