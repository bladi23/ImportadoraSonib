import { Routes } from '@angular/router';
import { ProductsComponent } from './features/catalog/products/products.component';
import { LoginComponent } from './features/auth/login/login.component';
import { AdminCategoriesComponent } from './features/admin/categories/admin-categories.component';
import { AdminProductsComponent } from './features/admin/products/admin-products.component';
import { CartComponent } from './features/cart/cart.component';
import { adminGuard } from './core/admin.guard';
import { ForgotComponent } from './features/auth/forgot/forgot.component';
import { ResetComponent } from './features/auth/reset/reset.component';
import { RegisterComponent } from './features/auth/register/register.component';


export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: ProductsComponent },
  { path: 'login', component: LoginComponent },
  { path: 'cart', component: CartComponent },
  { path: 'auth/forgot', component: ForgotComponent },
  { path: 'auth/reset', component: ResetComponent },
  { path: 'auth/register', component: RegisterComponent },
  { path: 'admin/categories', component: AdminCategoriesComponent, canActivate: [adminGuard] },
  { path: 'admin/products', component: AdminProductsComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'catalog' }
];
