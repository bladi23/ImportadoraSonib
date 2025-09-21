import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface DemoCreateSessionRes {
  checkoutUrl: string;
  sessionId: string;
  successUrl: string;
  cancelUrl: string;
}
export interface DemoConfirmRes { ok: boolean; status: string; reason?: string; }

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  demoCreateSession(orderId: number) {
    return this.http.post<DemoCreateSessionRes>(`${this.base}/payments/demo/create-session`, { orderId });
  }
  demoConfirm(orderId: number, sessionId: string, outcome: 'approved'|'declined'|'canceled') {
    return this.http.post<DemoConfirmRes>(`${this.base}/payments/demo/confirm`, { orderId, sessionId, outcome });
  }
  getOrder(orderId: number) {
    return this.http.get<any>(`${this.base}/payments/order/${orderId}`);
  }
}
