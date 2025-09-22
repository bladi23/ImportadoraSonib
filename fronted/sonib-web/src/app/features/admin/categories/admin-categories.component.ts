import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AdminApiService, AdminCategory } from '../../../core/admin-api.service';

@Component({
  selector: 'app-admin-categories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-categories.component.html',
  styleUrls: ['./admin-categories.component.scss']
})
export class AdminCategoriesComponent implements OnInit {
  // Inyectamos con 'inject' y NO usamos constructor, así no hay orden raro de inicialización
  private fb = inject(FormBuilder);
  private api = inject(AdminApiService);

  categories: AdminCategory[] = [];
  loading = false;
  error = '';
  editing: AdminCategory | null = null;

  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(80)]],
    slug: ['', [Validators.required, Validators.maxLength(80)]],
    isActive: [true]
  });

  ngOnInit() { this.load(); }

  load() {
    this.loading = true; this.error = '';
    this.api.listCategories().subscribe({
      next: (data) => { this.categories = data; this.loading = false; },
      error: () => { this.loading = false; this.error = 'No se pudo cargar categorías.'; }
    });
  }
   new() {
    this.editing = null;
    this.form.reset({ name: '', slug: '', isActive: true });
  }
  startCreate() {
    this.editing = null;
    this.form.reset({ name: '', slug: '', isActive: true });
  }

  startEdit(c: AdminCategory) {
    this.editing = c;
    this.form.reset({ name: c.name, slug: c.slug, isActive: c.isActive });
  }

  save() {
    if (this.form.invalid) return;
    const payload = this.form.value as any;

    if (this.editing) {
      this.api.updateCategory(this.editing.id, payload).subscribe({
        next: () => { this.startCreate(); this.load(); alert('Guardado'); },
        error: (e) => alert(e?.error || 'No se pudo guardar')
      });
    } else {
      this.api.createCategory(payload).subscribe({
        next: () => { this.startCreate(); this.load(); alert('Creado'); },
        error: (e) => alert(e?.error || 'No se pudo crear')
      });
    }
  }

  remove(c: AdminCategory) {
    if (!confirm(`¿Eliminar '${c.name}'? Si tiene productos, se desactivará.`)) return;
    this.api.deleteCategory(c.id).subscribe({
      next: () => { this.load(); },
      error: (e) => alert(e?.error || 'No se pudo eliminar')
    });
  }
}
