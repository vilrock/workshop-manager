import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { describeError } from '../../core/problem';
import { IconComponent } from '../../shared/icon.component';

interface DemoAccount {
  label: string;
  email: string;
  caption: string;
}

const DEMO_PASSWORD = 'Workshop#2026';

const WORKFLOW_STEPS: readonly string[] = ['Received', 'Diagnosed', 'Approved', 'In progress', 'Completed', 'Delivered'];

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, IconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly demoAccounts: readonly DemoAccount[] = [
    { label: 'Admin', email: 'admin@example.com', caption: 'Full access' },
    { label: 'Advisor', email: 'advisor@example.com', caption: 'Front desk' },
    { label: 'Mechanic', email: 'mechanic1@example.com', caption: 'Own jobs only' }
  ];
  protected readonly workflowSteps = WORKFLOW_STEPS;

  protected readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected useDemoAccount(account: DemoAccount): void {
    this.form.setValue({ email: account.email, password: DEMO_PASSWORD });
    this.errorMessage.set(null);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    const { email, password } = this.form.getRawValue();

    this.auth.login(email, password).subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: (error: unknown) => {
        const described = describeError(error);
        this.errorMessage.set(described.status === 401 ? 'The email or password is incorrect.' : described.detail);
        this.submitting.set(false);
      }
    });
  }
}
