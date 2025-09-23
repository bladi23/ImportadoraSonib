import { Component, EventEmitter, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { RouterLink } from '@angular/router'; 
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-login-panel',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login-panel.component.html',
  styleUrls: ['./login-panel.component.scss']
})
export class LoginPanelComponent {
  @Output() close = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  hide = true;
  loading = false;
  error = '';

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(120)]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  submit() {
    if (this.form.invalid) return;
    const { email, password } = this.form.value as any;
    this.loading = true; this.error = '';

    this.auth.login(email, password)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => {
         
          this.close.emit();
        },
        error: (e) => {
          const msg = e?.error ?? 'Credenciales inválidas.';
          this.error = typeof msg === 'string' ? msg : 'Credenciales inválidas.';
        }
      });
  }

  toggle() { this.hide = !this.hide; }
}
