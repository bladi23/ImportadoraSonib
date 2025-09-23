import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { finalize, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = false;
  error = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit() {
    this.error = '';
    if (this.form.invalid) return;

    const { email, password } = this.form.value as any;
    this.loading = true;

    this.auth.login(email, password)
   
      .pipe(
        switchMap(() => {
        
          return of(true);
        }),
        finalize(() => this.loading = false)
      )
      .subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.error = err?.error ?? 'Credenciales inválidas';
        }
      });
  }
}
