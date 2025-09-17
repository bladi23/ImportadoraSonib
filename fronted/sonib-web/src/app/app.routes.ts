import { Routes } from '@angular/router';
import { ProductsComponent } from './features/catalog/products/products.component';
import { LoginComponent } from './features/auth/login/login.component';

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: ProductsComponent },
  { path: 'login', component: LoginComponent },
  { path: '**', redirectTo: 'catalog' }
];
