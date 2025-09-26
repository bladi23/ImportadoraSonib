import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CartService } from '../../../core/cart.service';

// ✅ operadores
import { switchMap, catchError, tap, map } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';

type ProductDetail = {
  id: number;
  name: string;
  slug: string;
  description: string;
  price: number;
  imageUrl: string;
  stock: number;
  categoryId: number;
  category: string;
};

type RecoItem = {
  id: number;
  name: string;
  slug: string;
  price: number;
  imageUrl: string;
  stock: number;
  category: string;
};

@Component({
  standalone: true,
  selector: 'app-product-detail',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './product-detail.component.html',
  styleUrls: ['./product-detail.component.scss']
})
export class ProductDetailComponent {
  private http = inject(HttpClient);
  private cart = inject(CartService);
  private route = inject(ActivatedRoute);

  base = environment.apiBase;

  loading = true;
  error = '';
  p?: ProductDetail;
  qty = 1;

  recos: RecoItem[] = [];
  private readonly take = 6;

  ngOnInit() {
    this.route.paramMap.pipe(
      tap(() => { this.loading = true; this.error = ''; this.recos = []; }),
      switchMap(m => {
        const slug = m.get('slug')!;
        return this.http.get<ProductDetail>(`${this.base}/products/by-slug/${slug}`).pipe(
          tap(p => this.p = p),
          switchMap(p => this.loadMergedRecos(p.id, p.categoryId)),
          catchError(() => { this.error = 'Producto no encontrado.'; return of([] as RecoItem[]); })
        );
      })
    ).subscribe(list => {
      this.recos = list;
      this.loading = false;
    });
  }

  // ---------- Recos helpers (dedupe + fallback) ----------
  private fetchAlsoBought(productId: number) {
    return this.http
      .get<RecoItem[]>(`${this.base}/reco/also-bought/${productId}?take=${this.take}`)
      .pipe(catchError(() => of([] as RecoItem[])));
  }

  private fetchPopularInCategory(categoryId: number, excludeId: number) {
    // si añadiste exclude en el backend, úsalo; si no, igual filtramos en el front
    return this.http
      .get<RecoItem[]>(`${this.base}/reco/popular-in-category/${categoryId}?take=${this.take * 2}&exclude=${excludeId}`)
      .pipe(catchError(() => of([] as RecoItem[])));
  }

  private fetchPopularGlobal(excludeId: number) {
    return this.http
      .get<RecoItem[]>(`${this.base}/reco/popular?take=${this.take}`)
      .pipe(
        map(list => (list || []).filter(x => x.id !== excludeId)),
        catchError(() => of([] as RecoItem[]))
      );
  }

  private loadMergedRecos(productId: number, categoryId: number) {
    return forkJoin({
      ab: this.fetchAlsoBought(productId),
      pc: this.fetchPopularInCategory(categoryId, productId)
    }).pipe(
      switchMap(({ ab, pc }) => {
        const seen = new Set<number>([productId]); // no recomendar el mismo producto
        const merged: RecoItem[] = [];

        // 1º co-compra, 2º popular en categoría
        for (const it of [...(ab || []), ...(pc || [])]) {
          if (!it || seen.has(it.id)) continue;
          seen.add(it.id);
          merged.push(it);
          if (merged.length === this.take) break;
        }

        if (merged.length) return of(merged);

        // si no hay nada todavía, completar con popular global (excluyendo actual)
        return this.fetchPopularGlobal(productId).pipe(
          map(gl => gl.slice(0, this.take))
        );
      })
    );
  }
  // -------------------------------------------------------

  addToCart() {
    if (!this.p) return;
    const q = Math.max(1, Math.min(this.qty || 1, this.p.stock || 1));
    this.cart.add(this.p.id, q).subscribe({
      next: () => {}, // el servicio ya refresca el contador del header
      error: () => alert('No se pudo añadir al carrito')
    });
  }
}
