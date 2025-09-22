import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface AdminCategory { id: number; name: string; slug: string; isActive: boolean }
export interface AdminProduct {
  id: number; name: string; slug: string; description: string; tags: string;
  price: number; imageUrl: string; stock: number; isActive: boolean; isDeleted?: boolean;
  categoryId: number; category: string; rowVersion: string;
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private base = environment.apiBase;
  constructor(private http: HttpClient) {}

  // Categorías (admin)
  listCategories() { return this.http.get<AdminCategory[]>(`${this.base}/categories/admin`); }
  createCategory(b: {name:string; slug:string; isActive:boolean}) { return this.http.post(`${this.base}/categories`, b); }
  updateCategory(id: number, b: {name:string; slug:string; isActive:boolean}) { return this.http.put(`${this.base}/categories/${id}`, b); }
  deleteCategory(id: number) { return this.http.delete(`${this.base}/categories/${id}`); }

  // Productos (admin)
  listProducts(params: { search?: string; page?: number; pageSize?: number; includeDeleted?: boolean } = {}) {
    const q = new URLSearchParams();
    if (params.search) q.set('search', params.search);
    q.set('page', String(params.page || 1));
    q.set('pageSize', String(params.pageSize || 20));
    q.set('includeDeleted', String(params.includeDeleted ?? true));
    return this.http.get<{ total:number; page:number; pageSize:number; items:AdminProduct[] }>(`${this.base}/products/admin?${q}`);
  }
  getProduct(id: number) { return this.http.get<AdminProduct>(`${this.base}/products/admin/${id}`); }
  createProduct(b: Omit<AdminProduct,'id'|'category'|'rowVersion'|'isDeleted'>) { return this.http.post(`${this.base}/products`, b); }
  updateProduct(id: number, b: Omit<AdminProduct,'category'|'isDeleted'>) { return this.http.put(`${this.base}/products/${id}`, b, { observe:'response' }); }
  deleteProduct(id: number) { return this.http.delete(`${this.base}/products/${id}`); }

  // Subida de imagen
  uploadProductImage(file: File) {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.post<{url:string}>(`${this.base}/uploads/product-image`, fd);
  }
}
