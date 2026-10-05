export type UserRole = 'Admin' | 'Advisor' | 'Mechanic';

export type WorkOrderStatus = 'Received' | 'Diagnosed' | 'Approved' | 'InProgress' | 'Completed' | 'Delivered' | 'Cancelled';

export type LineItemType = 'Service' | 'Part';

export const WORK_ORDER_STATUSES: readonly WorkOrderStatus[] = ['Received', 'Diagnosed', 'Approved', 'InProgress', 'Completed', 'Delivered', 'Cancelled'];

export const STATUS_LABELS: Record<WorkOrderStatus, string> = {
  Received: 'Received',
  Diagnosed: 'Diagnosed',
  Approved: 'Approved',
  InProgress: 'In progress',
  Completed: 'Completed',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled'
};

export const TRANSITION_ACTION_LABELS: Record<WorkOrderStatus, string> = {
  Received: 'Reopen',
  Diagnosed: 'Mark as diagnosed',
  Approved: 'Approve quote',
  InProgress: 'Start work',
  Completed: 'Mark as completed',
  Delivered: 'Deliver to customer',
  Cancelled: 'Cancel order'
};

export interface User {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface Mechanic {
  userId: string;
  fullName: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAtUtc: string;
  user: User;
}

export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Customer {
  customerId: string;
  fullName: string;
  email: string;
  phone: string;
  address: string | null;
  isActive: boolean;
  vehicleCount: number;
  createdAtUtc: string;
}

export interface CustomerPayload {
  fullName: string;
  email: string;
  phone: string;
  address: string | null;
}

export interface Vehicle {
  vehicleId: string;
  customerId: string;
  customerName: string;
  licensePlate: string;
  make: string;
  model: string;
  year: number;
  vin: string | null;
  color: string | null;
  mileageKm: number;
}

export interface VehiclePayload {
  licensePlate: string;
  make: string;
  model: string;
  year: number;
  vin: string | null;
  color: string | null;
  mileageKm: number;
}

export interface WorkOrderSummary {
  workOrderId: string;
  orderNumber: string;
  status: WorkOrderStatus;
  customerId: string;
  customerName: string;
  vehicleId: string;
  vehicleDescription: string;
  description: string;
  assignedMechanicId: string | null;
  assignedMechanicName: string | null;
  totalAmount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  allowedTransitions: WorkOrderStatus[];
}

export interface LineItem {
  lineItemId: string;
  itemType: LineItemType;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface HistoryEntry {
  historyId: string;
  fromStatus: WorkOrderStatus | null;
  toStatus: WorkOrderStatus;
  changedByUserId: string;
  changedByName: string;
  changedAtUtc: string;
  correlationId: string;
  note: string | null;
}

export interface WorkOrderDetail extends WorkOrderSummary {
  mileageKm: number;
  createdByName: string;
  completedAtUtc: string | null;
  items: LineItem[];
  history: HistoryEntry[];
  canAssignMechanic: boolean;
  canAddItems: boolean;
}

export interface WorkOrderFilters {
  status?: WorkOrderStatus | '';
  mechanicId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateWorkOrderPayload {
  customerId: string;
  vehicleId: string;
  description: string;
  mileageKm: number;
}

export interface AddLineItemPayload {
  itemType: LineItemType;
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface StatusCount {
  status: WorkOrderStatus;
  count: number;
}

export interface MonthRevenue {
  month: string;
  revenue: number;
}

export interface DashboardStats {
  ordersByStatus: StatusCount[];
  openOrders: number;
  completedThisMonth: number;
  monthlyRevenue: number;
  averageHoursToComplete: number | null;
  averageHoursToDiagnose: number | null;
  revenueByMonth: MonthRevenue[];
  recentOrders: WorkOrderSummary[];
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
}
