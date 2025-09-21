import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { PaymentsService } from '../../core/payments.service';

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
  amount = '';
  loading = false;
  msg = '';

  constructor(private route: ActivatedRoute, private payments: PaymentsService) {
    this.route.queryParamMap.subscribe(p => {
      this.orderId = Number(p.get('orderId'));
      this.sessionId = p.get('sessionId') || '';
      this.amount = p.get('amount') || '';
    });
  }

  confirm(outcome: 'approved'|'declined'|'canceled') {
    this.loading = true; this.msg = '';
    this.payments.demoConfirm(this.orderId, this.sessionId, outcome).subscribe({
      next: (res) => {
        this.loading = false;
        this.msg = res.ok ? 'Pago aprobado (DEMO)' : (res.reason || ('Estado: ' + res.status));
      },
      error: () => { this.loading = false; this.msg = 'No se pudo confirmar el pago'; }
    });
  }
}
