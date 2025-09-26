import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

type MyOrder = { id:number; status:string; total:number; orderDate:string; paidAt?:string };

@Component({
  standalone: true,
  selector: 'app-my-orders',
  imports: [CommonModule],
  template: `
  <section>
    <h2>Mis pedidos</h2>
    <div *ngIf="loading">Cargando…</div>
    <p class="err" *ngIf="!loading && err">{{err}}</p>

    <table *ngIf="!loading && orders.length">
      <thead><tr><th>ID</th><th>Fecha</th><th>Estado</th><th>Total</th></tr></thead>
      <tbody>
        <tr *ngFor="let o of orders">
          <td>#{{o.id}}</td>
          <td>{{o.orderDate | date:'short'}}</td>
          <td>{{o.status}}</td>
          <td>$ {{o.total | number:'1.2-2'}}</td>
        </tr>
      </tbody>
    </table>

    <div *ngIf="!loading && !orders.length">Aún no tienes pedidos.</div>
  </section>
  `,
  styles: [`.err{color:#c00}`]
})
export class MyOrdersComponent {
  private http = inject(HttpClient);
  base = environment.apiBase;

  loading = true; err = ''; orders: MyOrder[] = [];

  ngOnInit() {
    this.http.get<MyOrder[]>(`${this.base}/orders/mine`).subscribe({
      next: r => { this.orders = r; this.loading = false; },
      error: () => { this.err = 'No se pudieron cargar los pedidos'; this.loading = false; }
    });
  }
}
