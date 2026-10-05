import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards';
import { UserRole } from './core/models';

const FRONT_DESK_ROLES: UserRole[] = ['Admin', 'Advisor'];

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in - WorkshopManager',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login.component').then((module) => module.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((module) => module.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard - WorkshopManager',
        loadComponent: () => import('./features/dashboard/dashboard.component').then((module) => module.DashboardComponent)
      },
      {
        path: 'customers',
        title: 'Customers - WorkshopManager',
        canActivate: [roleGuard],
        data: { roles: FRONT_DESK_ROLES },
        loadComponent: () => import('./features/customers/customers.component').then((module) => module.CustomersComponent)
      },
      {
        path: 'vehicles',
        title: 'Vehicles - WorkshopManager',
        canActivate: [roleGuard],
        data: { roles: FRONT_DESK_ROLES },
        loadComponent: () => import('./features/vehicles/vehicles.component').then((module) => module.VehiclesComponent)
      },
      {
        path: 'work-orders',
        title: 'Work orders - WorkshopManager',
        loadComponent: () => import('./features/work-orders/work-orders.component').then((module) => module.WorkOrdersComponent)
      },
      {
        path: 'work-orders/:workOrderId',
        title: 'Work order - WorkshopManager',
        loadComponent: () => import('./features/work-orders/work-order-detail.component').then((module) => module.WorkOrderDetailComponent)
      }
    ]
  },
  {
    path: '**',
    title: 'Not found - WorkshopManager',
    loadComponent: () => import('./features/not-found/not-found.component').then((module) => module.NotFoundComponent)
  }
];
