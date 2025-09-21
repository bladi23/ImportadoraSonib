import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CartService, CartItemVM } from '../../core/cart.service';
import { OrdersService } from '../../core/orders.service';
import { PaymentsService } from '../../core/payments.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.scss']
})
export class CartComponent implements OnInit {
  items: CartItemVM[] = [];
  total = 0;
  loading = false;
  paying = false;
  msg = '';

  constructor(
    private cart: CartService,
    private orders: OrdersService,
    private pay: PaymentsService
  ) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading = true;
    this.cart.get().subscribe({
      next: (res) => { this.items = res.items; this.total = res.total; this.loading = false; },
      error: () => { this.loading = false; this.msg = 'No se pudo cargar el carrito'; }
    });
  }

  remove(it: CartItemVM) {
    this.cart.remove(it.productId).subscribe({
      next: () => this.load(),
      error: (e) => alert('No se pudo eliminar: ' + (e?.error || 'error'))
    });
  }

  async pagarDemo() {
    if (!this.items.length) { alert('Carrito vacío'); return; }
    this.paying = true; this.msg = '';

    // 1) construir la orden
    const orderItems = this.items.map(i => ({ productId: i.productId, quantity: i.quantity }));
    this.orders.create({ items: orderItems }).subscribe({
      next: (o) => {
        // 2) crear sesión demo
        this.pay.demoCreateSession(o.orderId).subscribe({
          next: (s) => {
            // 3) redirigir al "checkout" demo del front
            window.location.href = s.checkoutUrl;
          },
          error: (e) => { this.paying = false; this.msg = 'No se pudo iniciar el pago DEMO'; }
        });
      },
      error: (e) => { this.paying = false; this.msg = 'No se pudo crear la orden'; }
    });
  }
}
