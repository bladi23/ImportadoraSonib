import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrdersService } from '../../../core/orders.service';

@Component({
  standalone: true,
  selector: 'app-my-orders',
  imports: [CommonModule, RouterLink],
  template: `
  <section>
    <h2>Mis pedidos</h2>
    <div *ngIf="loading">Cargando…</div>
    <p class="err" *ngIf="!loading && err">{{err}}</p>

    <table *ngIf="!loading && orders.length">
      <thead><tr><th>ID</th><th>Fecha</th><th>Estado</th><th class="num">Total</th></tr></thead>
      <tbody>
        <tr *ngFor="let o of orders">
          <td><a [routerLink]="['/orders', o.id]">#{{o.id}}</a></td>
          <td>{{o.orderDate | date:'short'}}</td>
          <td>{{o.status}}</td>
          <td class="num">$ {{o.total | number:'1.2-2'}}</td>
        </tr>
      </tbody>
    </table>

    <div *ngIf="!loading && !orders.length">Aún no tienes pedidos.</div>
  </section>
  `,
  styles: [`.err{color:#c00}.num{text-align:right}`]
})
export class MyOrdersComponent {
  private svc = inject(OrdersService);

  loading = true; err = ''; orders: any[] = [];

  ngOnInit() {
    this.svc.my().subscribe({
      next: r => { this.orders = r; this.loading = false; },
      error: () => { this.err = 'No se pudieron cargar los pedidos'; this.loading = false; }
    });
  }
}
