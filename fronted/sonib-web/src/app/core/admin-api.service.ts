import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface AdminCategory {
  id: number;
  name: string;
  slug: string;
  isActive: boolean;
}

export interface AdminProduct {
  id: number;
  name: string;
  slug: string;
  description: string;
  tags: string;
  price: number;
  imageUrl: string;
  stock: number;
  isActive: boolean;
  isDeleted?: boolean;
  categoryId: number;
  category: string;
  rowVersion: string; // base64 desde el backend
}

export interface CreateProductReq {
  name: string;
  slug: string;
  description?: string;
  tags?: string;
  price: number;
  imageUrl?: string;
  stock: number;
  isActive: boolean;
  categoryId: number;
}

export interface UpdateProductReq {
  name: string;
  slug: string;
  description?: string;
  tags?: string;
  price: number;
  imageUrl?: string;
  stock: number;
  isActive: boolean;
  categoryId: number;
  rowVersion: string; // requerido para concurrencia
}

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private base = environment.apiBase; // ej: https://localhost:7234/api
  constructor(private http: HttpClient) {}

  // ==================== Categorías (ADMIN) ====================
  // Requiere tener AdminCategoriesController en /api/admin/categories
  listCategories() {
    return this.http.get<AdminCategory[]>(`${this.base}/admin/categories`);
  }
  createCategory(b: { name: string; slug: string; isActive: boolean }) {
    return this.http.post(`${this.base}/admin/categories`, b);
  }
  updateCategory(id: number, b: { name: string; slug: string; isActive: boolean }) {
    return this.http.put(`${this.base}/admin/categories/${id}`, b);
  }
  deleteCategory(id: number) {
    return this.http.delete(`${this.base}/admin/categories/${id}`);
  }

  // ==================== Productos (ADMIN) ====================
  listProducts(params: { search?: string; page?: number; pageSize?: number; includeDeleted?: boolean } = {}) {
    let httpParams = new HttpParams()
      .set('page', String(params.page ?? 1))
      .set('pageSize', String(params.pageSize ?? 20))
      .set('includeDeleted', String(params.includeDeleted ?? true));
    if (params.search) httpParams = httpParams.set('search', params.search);

    return this.http.get<{ total: number; page: number; pageSize: number; items: AdminProduct[] }>(
      `${this.base}/admin/products`,
      { params: httpParams }
    );
  }

  getProduct(id: number) {
    return this.http.get<AdminProduct>(`${this.base}/admin/products/${id}`);
  }

  createProduct(b: CreateProductReq) {
    return this.http.post(`${this.base}/admin/products`, b);
  }

  updateProduct(id: number, b: UpdateProductReq) {
    // Observa la respuesta por si el backend devuelve 409 con el “current”
    return this.http.put(`${this.base}/admin/products/${id}`, b, { observe: 'response' });
  }

  deleteProduct(id: number) {
    return this.http.delete(`${this.base}/admin/products/${id}`);
  }

  // Subir imagen del producto
 uploadProductImage(file: File) {
  const fd = new FormData();
  fd.append('file', file, file.name);
  return this.http.post<{ url: string }>(`${this.base}/uploads/product-image`, fd);
}

}
