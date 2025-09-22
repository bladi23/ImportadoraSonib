import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { AdminApiService, AdminCategory, AdminProduct } from '../../../core/admin-api.service';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './admin-products.component.html',
  styleUrls: ['./admin-products.component.scss']
})
export class AdminProductsComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(AdminApiService);

  categories: AdminCategory[] = [];
  products: AdminProduct[] = [];
  total = 0; page = 1; pageSize = 20; search = '';
  loading = false; error = ''; editing: AdminProduct|null = null; preview = '';

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(150)]],
    slug: ['', [Validators.required, Validators.maxLength(150)]],
    description: ['',[Validators.maxLength(500)]],
    tags: ['',[Validators.maxLength(200)]],
    price: [0, [Validators.required, Validators.min(0)]],
    imageUrl: ['',[Validators.maxLength(300)]],
    stock: [0, [Validators.required, Validators.min(0)]],
    isActive: [true],
    categoryId: [0, [Validators.required]],
    rowVersion: [''] // solo para update (concurrencia)
  });

  ngOnInit(){
    this.api.listCategories().subscribe({
      next: cs => this.categories = cs,
      error: () => this.error = 'No se pudieron cargar categorías.'
    });
    this.load();
  }

  load(){
    this.loading = true; this.error = '';
    this.api.listProducts({ search: this.search, page: this.page, pageSize: this.pageSize, includeDeleted: true })
      .subscribe({
        next: r => { this.products = r.items; this.total = r.total; this.loading = false; },
        error: () => { this.loading = false; this.error = 'No se pudo cargar productos.'; }
      });
  }

  new(){
    this.editing = null; this.preview = '';
    this.form.reset({
      name:'', slug:'', description:'', tags:'',
      price:0, imageUrl:'', stock:0, isActive:true, categoryId:0, rowVersion:''
    });
  }

  edit(p: AdminProduct){
    this.editing = p; this.preview = p.imageUrl || '';
    this.api.getProduct(p.id).subscribe({
      next: d => {
        this.form.reset({
          name:d.name, slug:d.slug, description:d.description, tags:d.tags,
          price:d.price, imageUrl:d.imageUrl, stock:d.stock, isActive:d.isActive,
          categoryId:d.categoryId, rowVersion:d.rowVersion
        });
      },
      error: () => alert('No se pudo cargar el detalle del producto')
    });
  }

  onFile(e: Event){
    const input = e.target as HTMLInputElement;
    const f = input?.files?.[0];
    if (!f) return;
    this.api.uploadProductImage(f).subscribe({
      next: res => { this.form.patchValue({ imageUrl: res.url }); this.preview = res.url; },
      error: err => alert(err?.error || 'No se pudo subir la imagen')
    });
  }

  save(){
    if (this.form.invalid) return;
    const v = this.form.value as any;

    if (this.editing){
      // UPDATE con RowVersion (concurrencia)
      const body = { ...v, id: this.editing.id };
      this.api.updateProduct(this.editing.id, body).subscribe({
        next: (resp) => {
          if (resp.status === 204) { alert('Guardado'); this.new(); this.load(); }
        },
        error: (err) => {
          if (err.status === 409 && err.error?.current){
            const c = err.error.current as AdminProduct;
            alert('Otro usuario modificó este producto. Se cargó la versión más reciente.');
            this.editing = c; this.preview = c.imageUrl || '';
            this.form.reset({
              name:c.name, slug:c.slug, description:c.description, tags:c.tags,
              price:c.price, imageUrl:c.imageUrl, stock:c.stock, isActive:c.isActive,
              categoryId:c.categoryId, rowVersion:c.rowVersion
            });
          } else {
            alert(err?.error || 'No se pudo guardar');
          }
        }
      });
    } else {
      // CREATE (sin RowVersion)
      const body = { ...v }; delete (body as any).rowVersion;
      this.api.createProduct(body).subscribe({
        next: () => { alert('Creado'); this.new(); this.load(); },
        error: (err) => alert(err?.error || 'No se pudo crear')
      });
    }
  }

  remove(p: AdminProduct){
    if (!confirm(`¿Eliminar '${p.name}'? (borrado lógico)`)) return;
    this.api.deleteProduct(p.id).subscribe({
      next: () => this.load(),
      error: (err) => alert(err?.error || 'No se pudo eliminar')
    });
  }
}
