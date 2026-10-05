import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

export interface DescribedError {
  status: number;
  title: string;
  detail: string;
  fieldErrors: Record<string, string[]>;
}

const FALLBACK_TITLE = 'Something went wrong';

export function describeError(error: unknown): DescribedError {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, title: FALLBACK_TITLE, detail: 'Unexpected client error.', fieldErrors: {} };
  }

  if (error.status === 0) {
    return { status: 0, title: 'Server unreachable', detail: 'Check your connection and try again.', fieldErrors: {} };
  }

  const problem = (typeof error.error === 'object' && error.error !== null ? error.error : {}) as ProblemDetails;
  return {
    status: error.status,
    title: problem.title ?? FALLBACK_TITLE,
    detail: problem.detail ?? defaultDetail(error.status),
    fieldErrors: toCamelCaseKeys(problem.errors ?? {})
  };
}

export function firstFieldError(error: DescribedError): string | null {
  const first = Object.values(error.fieldErrors)[0];
  return first?.[0] ?? null;
}

function defaultDetail(status: number): string {
  if (status === 429) {
    return 'Too many attempts. Wait a minute and try again.';
  }
  return status >= 500 ? 'The server could not complete the request.' : 'The request could not be completed.';
}

function toCamelCaseKeys(errors: Record<string, string[]>): Record<string, string[]> {
  return Object.fromEntries(Object.entries(errors).map(([key, value]) => [key.charAt(0).toLowerCase() + key.slice(1), value]));
}
