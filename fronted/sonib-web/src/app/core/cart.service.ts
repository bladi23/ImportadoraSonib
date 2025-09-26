import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CartItemVM {
  id: number;
  productId: number;
  productName: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
  createdAt: string;
}
export interface CartRes { total: number; items: CartItemVM[]; }

@Injectable({ providedIn: 'root' })
export class CartService {
  private base = environment.apiBase;

  /** contador público para header */
  private _count = new BehaviorSubject<number>(0);
  readonly count$ = this._count.asObservable();

  constructor(private http: HttpClient) {}

  /** Lee carrito y actualiza contador */
  refresh() {
    this.get().subscribe({
      next: r => this._count.next(r.items?.length ?? 0),
      error: () => this._count.next(0)
    });
  }

  get() { return this.http.get<CartRes>(`${this.base}/cartitems`); }

  add(productId: number, qty = 1) {
    return this.http.post(`${this.base}/cartitems`, { productId, qty })
      .pipe(tap(() => this.refresh()));
  }

  remove(productId: number) {
    return this.http.delete(`${this.base}/cartitems/${productId}`)
      .pipe(tap(() => this.refresh()));
  }
  clear() {
  return this.http.delete(`${this.base}/cartitems/all`)
    .pipe(tap(() => this.refresh()));
}
  
}
