import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

export interface LoginRes { token: string; email: string }
export interface MeRes { email: string; userId?: string; roles: string[] }
export interface Category { id: number; name: string; slug: string }
export interface ProductListItem { id: number; name: string; slug: string; price: number; imageUrl: string; stock: number; category: string }
export interface Paged<T> { total: number; page: number; pageSize: number; items: T[] }

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  login(email: string, password: string) { return this.http.post<LoginRes>(`${this.base}/auth/login`, { email, password }); }
  register(email: string, password: string) { return this.http.post(`${this.base}/auth/register`, { email, password }); }
  me() { return this.http.get<MeRes>(`${this.base}/auth/me`); }

  getCategories() { return this.http.get<Category[]>(`${this.base}/categories`); }
  getProducts(options: { category?: string; page?: number; pageSize?: number; search?: string }): Observable<Paged<ProductListItem>> {
    let params = new HttpParams();
    if (options.category) params = params.set('category', options.category);
    if (options.page) params = params.set('page', options.page);
    if (options.pageSize) params = params.set('pageSize', options.pageSize);
    if (options.search) params = params.set('search', options.search);
    return this.http.get<Paged<ProductListItem>>(`${this.base}/products`, { params });
  }
 
}
