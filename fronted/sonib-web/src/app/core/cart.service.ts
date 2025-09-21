import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

export interface CartRes {
  total: number;
  items: CartItemVM[];
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  get() {
    return this.http.get<CartRes>(`${this.base}/cartitems`);
  }
  add(productId: number, qty = 1) {
    return this.http.post(`${this.base}/cartitems`, { productId, qty });
  }
  remove(productId: number) {
    return this.http.delete(`${this.base}/cartitems/${productId}`);
  }
}
