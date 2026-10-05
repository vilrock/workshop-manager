import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../core/auth.service';
import { UserRole } from '../core/models';
import { IconComponent } from '../shared/icon.component';
import { ToastContainerComponent } from '../shared/toast-container.component';

interface NavItem {
  label: string;
  path: string;
  icon: string;
  roles: readonly UserRole[];
}

const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: 'dashboard', roles: ['Admin', 'Advisor', 'Mechanic'] },
  { label: 'Work orders', path: '/work-orders', icon: 'clipboard', roles: ['Admin', 'Advisor', 'Mechanic'] },
  { label: 'Customers', path: '/customers', icon: 'users', roles: ['Admin', 'Advisor'] },
  { label: 'Vehicles', path: '/vehicles', icon: 'car', roles: ['Admin', 'Advisor'] }
];

const ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Administrator',
  Advisor: 'Service advisor',
  Mechanic: 'Mechanic'
};

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, IconComponent, ToastContainerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  protected readonly navOpen = signal(false);

  protected readonly navItems = computed(() => NAV_ITEMS.filter((item) => this.auth.hasRole(item.roles)));
  protected readonly roleLabel = computed(() => ROLE_LABELS[this.auth.role() ?? 'Mechanic']);
  protected readonly initials = computed(() =>
    (this.auth.user()?.fullName ?? '')
      .split(' ')
      .map((part) => part.charAt(0))
      .slice(0, 2)
      .join('')
      .toUpperCase()
  );

  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  protected readonly pageTitle = computed(() => {
    const url = this.currentUrl();
    return this.navItems().find((item) => url.startsWith(item.path))?.label ?? 'WorkshopManager';
  });

  constructor() {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => this.navOpen.set(false));
  }

  protected toggleNav(): void {
    this.navOpen.update((open) => !open);
  }

  protected logout(): void {
    this.auth.logout();
  }
}
