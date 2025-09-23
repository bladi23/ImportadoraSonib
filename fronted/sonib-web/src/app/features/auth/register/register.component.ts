import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth.service';
import { finalize, switchMap } from 'rxjs/operators';

function samePassword(c: AbstractControl) {
  const p = c.get('password')?.value;
  const r = c.get('confirm')?.value;
  return p && r && p === r ? null : { mismatch: true };
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  loading = false;
  error = '';
  ok = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    passwordGroup: this.fb.group({
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirm: ['', [Validators.required]]
    }, { validators: samePassword })
  });

  submit() {
    this.error = '';
    if (this.form.invalid) return;
    const email = this.form.value.email!;
    const password = this.form.value.passwordGroup?.password!;

    this.loading = true;
    
    this.auth.register(email, password).pipe(
      // tras registrarse, iniciamos sesión
      switchMap(() => this.auth.login(email, password)),
      switchMap(() => this.auth.afterLogin ? this.auth.afterLogin() : []),
      finalize(() => this.loading = false)
    ).subscribe({
      next: () => { this.ok = true; this.router.navigate(['/']); },
      error: (e) => { this.error = e?.error ?? 'No se pudo registrar.'; }
    });
  }
}
