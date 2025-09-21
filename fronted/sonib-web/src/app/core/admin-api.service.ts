import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface AdminCategory { id: number; name: string; slug: string; isActive: boolean }

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  // Categorías
  listCategories() {
    return this.http.get<AdminCategory[]>(`${this.base}/categories/admin`);
  }
  createCategory(body: { name: string; slug: string; isActive: boolean }) {
    return this.http.post(`${this.base}/categories`, body);
  }
  updateCategory(id: number, body: { name: string; slug: string; isActive: boolean }) {
    return this.http.put(`${this.base}/categories/${id}`, body);
  }
  deleteCategory(id: number) {
    return this.http.delete(`${this.base}/categories/${id}`);
  }
}
