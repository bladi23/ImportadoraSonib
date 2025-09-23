import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-forgot',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './forgot.component.html'
})
export class ForgotComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  sent = false; error = ''; loading = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit(){
    if (this.form.invalid) return;
    this.loading = true; this.error = '';
    const { email } = this.form.value as any;
    this.auth.forgot(email).subscribe({
      next: () => { this.loading=false; this.sent=true; },
      error: (e) => { this.loading=false; this.error = e?.error || 'No se pudo enviar el correo.'; }
    });
  }
}
