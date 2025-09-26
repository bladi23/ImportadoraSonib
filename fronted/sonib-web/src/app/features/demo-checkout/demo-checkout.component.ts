import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule, Router } from '@angular/router';
import { PaymentsService } from '../../core/payments.service';
import { CartService } from '../../core/cart.service';
import { finalize } from 'rxjs/operators';

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
      // Acepta amount o total (por si cambias en el futuro)
      this.amount = p.get('amount') || p.get('total') || '';

      if (!this.orderId || !this.sessionId) {
        this.msg = 'Sesión de pago inválida.';
      }
    });
  }

  confirm(outcome: 'approved'|'declined'|'canceled') {
    if (!this.orderId || !this.sessionId) return;

    this.loading = true; 
    this.msg = '';

    this.payments.demoConfirm(this.orderId, this.sessionId, outcome)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (res) => {
          if (outcome === 'approved' && res.ok) {
            // ✅ vaciamos contador del header (el backend ya limpió el carrito)
            this.cart.refresh();
            this.router.navigate(['/checkout/success'], { queryParams: { orderId: this.orderId } });
            return;
          }

          if (outcome === 'canceled') {
            this.router.navigate(['/checkout/cancel'], { queryParams: { orderId: this.orderId } });
            return;
          }

          // rechazado o cualquier otro estado
          this.msg = res.reason || ('Estado: ' + res.status);
        },
        error: (err) => {
          const beMsg = err?.error?.message || (typeof err?.error === 'string' ? err.error : '');
          this.msg = beMsg || 'No se pudo confirmar el pago';
        }
      });
  }
}
