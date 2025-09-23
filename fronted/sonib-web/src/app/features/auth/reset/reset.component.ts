import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../../core/auth.service';

@Component({
  selector: 'app-reset',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './reset.component.html',
})
export class ResetComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  loading=false; error=''; ok=false;
  email = this.route.snapshot.queryParamMap.get('email') || '';
  code = this.route.snapshot.queryParamMap.get('code') || '';

  form = this.fb.group({
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  submit(){
    if (this.form.invalid) return;
    this.loading = true; this.error = '';
    const { password } = this.form.value as any;
    this.auth.reset(this.code, this.email, password).subscribe({
      next: () => { this.loading=false; this.ok=true; setTimeout(()=>this.router.navigate(['/login']), 800); },
      error: e => { this.loading=false; this.error = e?.error || 'No se pudo cambiar la contraseña.'; }
    });
  }
}
