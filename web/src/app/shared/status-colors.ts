import { WorkOrderStatus } from '../core/models';

export const STATUS_COLORS: Record<WorkOrderStatus, string> = {
  Received: 'var(--muted)',
  Diagnosed: 'var(--info)',
  Approved: 'var(--violet)',
  InProgress: 'var(--accent)',
  Completed: 'var(--success)',
  Delivered: '#2d6a64',
  Cancelled: 'var(--danger)'
};
