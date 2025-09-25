import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment'; // 👈 ruta correcta
import { CartService, CartItemVM, CartRes } from '../../core/cart.service';

@Component({
  standalone: true,
  selector: 'app-cart',
  imports: [CommonModule],
  templateUrl: './cart.component.html',
  styleUrls: ['./cart.component.scss']
})
export class CartComponent {
  private http = inject(HttpClient);
  private cartSvc = inject(CartService);
  private router = inject(Router);

  base = environment.apiBase;

  loading = true;
  items: CartItemVM[] = [];
  total = 0;
  msg = ''; // 👈 evita el error en el template

  ngOnInit() { this.refresh(); }

  refresh() {
    this.msg = '';
    this.loading = true;
    this.cartSvc.get().subscribe({
      next: (res: CartRes) => {
        this.items = res.items;
        this.total = res.total;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.msg = 'No se pudo cargar el carrito.';
      }
    });
  }

  // Recibe productId, no el item completo
  remove(productId: number) {
    this.cartSvc.remove(productId).subscribe({
      next: () => {
        this.refresh();
        this.cartSvc.refresh(); // actualiza contador en el header
      },
      error: () => this.msg = 'No se pudo quitar el producto.'
    });
  }

  // Flujo demo de tarjeta
payDemo() {
  if (!this.items.length) return;

  const payload = { items: this.items.map(i => ({ productId: i.productId, quantity: i.quantity })) };

  // 1) Crea la orden
  this.http.post<{ orderId:number; total:number }>(`${this.base}/orders`, payload).subscribe({
    next: (o) => {
      // 2) Crea la sesión DEMO
      this.http.post<{ sessionId:string; checkoutUrl:string }>(
        `${this.base}/payments/demo/create-session`,
        { orderId: o.orderId }
      ).subscribe({
        next: (s) => {
          // 3) Ir al checkout demo con orderId + sessionId + amount
          this.router.navigate(['/demo-checkout'], {
            queryParams: {
              orderId: o.orderId,
              sessionId: s.sessionId,
              amount: o.total.toFixed(2) // 👈 tu componente espera 'amount'
            }
          });
        },
        error: () => this.msg = 'No se pudo iniciar la sesión de pago.'
      });
    },
    error: (err) => {
      if (err?.status === 401) {
        this.router.navigate(['/login'], { queryParams: { returnUrl: '/cart' }});
      } else {
        this.msg = 'No se pudo crear la orden.';
      }
    }
  });
}



   
   payWhatsApp() {
     const payload = { items: this.items.map(i => ({ productId: i.productId, quantity: i.quantity })) };
     this.http.post<{ orderId:number; total:number; whatsappUrl:string }>(`${this.base}/orders`, payload)
       .subscribe({
       next: (res) => {
          window.open(res.whatsappUrl, '_blank');
           this.refresh();
         this.cartSvc.refresh();
        },
        error: () => this.msg = 'No se pudo crear el pedido por WhatsApp.'
      });
   }
}
