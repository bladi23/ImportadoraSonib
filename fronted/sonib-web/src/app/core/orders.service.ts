import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface CreateOrderReq { items: { productId: number; quantity: number }[]; }
export interface CreateOrderRes { orderId: number; total: number; whatsappUrl?: string; }

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  create(body: CreateOrderReq) {
    return this.http.post<CreateOrderRes>(`${this.base}/orders`, body);
  }
}
