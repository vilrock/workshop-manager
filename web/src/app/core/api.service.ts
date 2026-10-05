import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AddLineItemPayload,
  CreateWorkOrderPayload,
  Customer,
  CustomerPayload,
  DashboardStats,
  LoginResponse,
  Mechanic,
  Paged,
  User,
  Vehicle,
  VehiclePayload,
  WorkOrderDetail,
  WorkOrderFilters,
  WorkOrderStatus,
  WorkOrderSummary
} from './models';

const API_ROOT = '/api/v1';

type QueryValue = string | number | boolean | null | undefined;

function toParams(values: Record<string, QueryValue>): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(values)) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${API_ROOT}/auth/login`, { email, password });
  }

  currentUser(): Observable<User> {
    return this.http.get<User>(`${API_ROOT}/auth/me`);
  }

  listCustomers(search: string, page: number, pageSize: number): Observable<Paged<Customer>> {
    return this.http.get<Paged<Customer>>(`${API_ROOT}/customers`, { params: toParams({ search, page, pageSize }) });
  }

  createCustomer(payload: CustomerPayload): Observable<Customer> {
    return this.http.post<Customer>(`${API_ROOT}/customers`, payload);
  }

  updateCustomer(customerId: string, payload: CustomerPayload & { isActive: boolean }): Observable<Customer> {
    return this.http.put<Customer>(`${API_ROOT}/customers/${customerId}`, payload);
  }

  listCustomerVehicles(customerId: string): Observable<Vehicle[]> {
    return this.http.get<Vehicle[]>(`${API_ROOT}/customers/${customerId}/vehicles`);
  }

  createVehicle(customerId: string, payload: VehiclePayload): Observable<Vehicle> {
    return this.http.post<Vehicle>(`${API_ROOT}/customers/${customerId}/vehicles`, payload);
  }

  listVehicles(search: string, customerId: string, page: number, pageSize: number): Observable<Paged<Vehicle>> {
    return this.http.get<Paged<Vehicle>>(`${API_ROOT}/vehicles`, { params: toParams({ search, customerId, page, pageSize }) });
  }

  updateVehicle(vehicleId: string, payload: VehiclePayload): Observable<Vehicle> {
    return this.http.put<Vehicle>(`${API_ROOT}/vehicles/${vehicleId}`, payload);
  }

  listMechanics(): Observable<Mechanic[]> {
    return this.http.get<Mechanic[]>(`${API_ROOT}/users/mechanics`);
  }

  listWorkOrders(filters: WorkOrderFilters): Observable<Paged<WorkOrderSummary>> {
    const params = toParams({ ...filters });
    return this.http.get<Paged<WorkOrderSummary>>(`${API_ROOT}/work-orders`, { params });
  }

  getWorkOrder(workOrderId: string): Observable<WorkOrderDetail> {
    return this.http.get<WorkOrderDetail>(`${API_ROOT}/work-orders/${workOrderId}`);
  }

  createWorkOrder(payload: CreateWorkOrderPayload, idempotencyKey: string): Observable<WorkOrderDetail> {
    const headers = new HttpHeaders({ 'Idempotency-Key': idempotencyKey });
    return this.http.post<WorkOrderDetail>(`${API_ROOT}/work-orders`, payload, { headers });
  }

  assignMechanic(workOrderId: string, mechanicId: string): Observable<WorkOrderDetail> {
    return this.http.put<WorkOrderDetail>(`${API_ROOT}/work-orders/${workOrderId}/mechanic`, { mechanicId });
  }

  transitionWorkOrder(workOrderId: string, toStatus: WorkOrderStatus, expectedStatus: WorkOrderStatus, note: string | null): Observable<WorkOrderDetail> {
    return this.http.post<WorkOrderDetail>(`${API_ROOT}/work-orders/${workOrderId}/transitions`, { toStatus, expectedStatus, note });
  }

  addLineItem(workOrderId: string, payload: AddLineItemPayload): Observable<WorkOrderDetail> {
    return this.http.post<WorkOrderDetail>(`${API_ROOT}/work-orders/${workOrderId}/items`, payload);
  }

  dashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${API_ROOT}/dashboard/stats`);
  }
}
