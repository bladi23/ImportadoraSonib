export interface LoginRes { token: string; email: string }
export interface MeRes { email: string; userId?: string; roles: string[] }
export interface Category { id: number; name: string; slug: string }
export interface ProductListItem { id: number; name: string; slug: string; price: number; imageUrl: string; stock: number; category: string }
export interface Paged<T> { total: number; page: number; pageSize: number; items: T[] }