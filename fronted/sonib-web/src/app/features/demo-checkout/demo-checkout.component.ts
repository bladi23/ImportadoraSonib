import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { PaymentsService } from '../../core/payments.service';
import { CartService } from '../../core/cart.service';

@Component({
  selector: 'app-demo-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './demo-checkout.component.html',
  styleUrls: ['./demo-checkout.component.scss']
})
export class DemoCheckoutComponent {
  orderId!: number;
  sessionId = '';
  amount = ''; // string para mostrar tal cual
  loading = false;
  msg = '';

  constructor(
    private route: ActivatedRoute,
    private payments: PaymentsService,
    private router: Router,
    private cart: CartService
  ) {
    this.route.queryParamMap.subscribe(p => {
      this.orderId  = Number(p.get('orderId'));
      this.sessionId = p.get('sessionId') || '';
      // Acepta amount o total (por si cambiaste en el futuro)
      this.amount = p.get('amount') || p.get('total') || '';

      if (!this.orderId || !this.sessionId) {
        this.msg = 'Sesión de pago inválida.';
      }
    });
  }

  confirm(outcome: 'approved'|'declined'|'canceled') {
    if (!this.orderId || !this.sessionId) { this.msg = 'Sesión de pago inválida.'; return; }

    this.loading = true; this.msg = '';
    this.payments.demoConfirm(this.orderId, this.sessionId, outcome).subscribe({
      next: (res) => {
        this.loading = false;

        if (res.ok) {
          this.msg = 'Pago aprobado (DEMO). Redirigiendo...';
          // Refresca contador del carrito; si además tu backend lo limpia al pagar,
          // esto mostrará el nuevo estado.
          this.cart.refresh();

          // Redirige al carrito (puedes ajustar a otra página si quieres)
          setTimeout(() => this.router.navigate(['/cart']), 800);
        } else {
          this.msg = res.reason || ('Estado: ' + res.status);
        }
      },
      error: (e) => {
        this.loading = false;
        if (e?.status === 401) {
          this.router.navigate(['/login'], { queryParams: { returnUrl: '/cart' }});
        } else {
          this.msg = 'No se pudo confirmar el pago';
        }
      }
    });
  }
}
