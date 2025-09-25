import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CartService } from '../../../core/cart.service';
import { switchMap, catchError, of, tap } from 'rxjs';



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

 
ngOnInit() {
  this.route.paramMap.pipe(
    tap(() => { this.loading = true; this.error = ''; }),
    switchMap(m => {
      const slug = m.get('slug')!;
      return this.http.get<ProductDetail>(`${this.base}/products/by-slug/${slug}`).pipe(
        catchError(() => { this.error = 'Producto no encontrado.'; return of(null as any); })
      );
    })
  ).subscribe(p => {
    this.p = p || undefined;
    this.loading = false;
  });
}

 addToCart() {
  if (!this.p) return;
  const q = Math.max(1, Math.min(this.qty || 1, this.p.stock || 1));
  this.cart.add(this.p.id, q).subscribe({
    next: () => {}, // el servicio ya hace refresh del contador
    error: () => alert('No se pudo añadir al carrito')
  });
}
}
