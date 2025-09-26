import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export type MyOrderRow = { id:number; status:string; total:number; orderDate:string };

@Injectable({ providedIn: 'root' })
export class OrdersService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  my() { return this.http.get<MyOrderRow[]>(`${this.base}/orders/my`); }
  get(id: number) { return this.http.get<any>(`${this.base}/payments/order/${id}`); }
}
