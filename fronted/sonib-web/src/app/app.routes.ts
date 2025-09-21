import { Routes } from '@angular/router';
import { ProductsComponent } from './features/catalog/products/products.component';
import { LoginComponent } from './features/auth/login/login.component';
import { CartComponent } from './features/cart/cart.component';
import { DemoCheckoutComponent } from './features/demo-checkout/demo-checkout.component';
import { AdminCategoriesComponent } from './features/admin/categories/admin-categories.component';
import { adminGuard } from './core/admin.guard';



export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: ProductsComponent },
  { path: 'login', component: LoginComponent },
   { path: 'cart', component: CartComponent },
  { path: 'demo-checkout', component: DemoCheckoutComponent },
  { path: 'admin/categories', component: AdminCategoriesComponent, canActivate: [adminGuard] },
  { path: '**', redirectTo: 'catalog' }
];
