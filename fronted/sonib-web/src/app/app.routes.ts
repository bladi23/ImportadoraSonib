import { Routes } from '@angular/router';
import { ProductsComponent } from './features/catalog/products/products.component';
import { LoginComponent } from './features/auth/login/login.component';
import { AdminCategoriesComponent } from './features/admin/categories/admin-categories.component';
import { AdminProductsComponent } from './features/admin/products/admin-products.component';
import { adminGuard } from './core/admin.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: ProductsComponent },
  { path: 'login', component: LoginComponent },
  { path: 'admin/categories', component: AdminCategoriesComponent, canActivate: [adminGuard] },
  { path: 'admin/products', component: AdminProductsComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'catalog' }
];
