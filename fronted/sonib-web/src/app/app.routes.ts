import { Routes } from '@angular/router';
import { ProductsComponent } from './features/catalog/products/products.component';
import { LoginComponent } from './features/auth/login/login.component';
import { CartComponent } from './features/cart/cart.component';
import { adminGuard } from './core/admin.guard';
import { ForgotComponent } from './features/auth/forgot/forgot.component';
import { ResetComponent } from './features/auth/reset/reset.component';
import { RegisterComponent } from './features/auth/register/register.component';

// ⛳️ Nota: NO importes AdminCategoriesComponent / AdminProductsComponent aquí
// si vas a usar loadComponent (lazy) en las rutas admin.

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },

  // Públicas
  { path: 'catalog', component: ProductsComponent },
  { path: 'login', component: LoginComponent },
  { path: 'cart', component: CartComponent },


  // Auth
  { path: 'auth/forgot', component: ForgotComponent },
  { path: 'auth/reset', component: ResetComponent },
  { path: 'auth/register', component: RegisterComponent },

  // ✅ Alias para enlaces antiguos que apuntan a /register
  { path: 'register', redirectTo: 'auth/register' },

  // Admin (lazy + guard)
  {
    path: 'admin/categories',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/categories/admin-categories.component')
        .then(m => m.AdminCategoriesComponent)
  },
  {
    path: 'admin/products',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/products/admin-products.component')
        .then(m => m.AdminProductsComponent)
  },
  {
  path: 'product/:slug',
  loadComponent: () =>
    import('./features/catalog/product-detail/product-detail.component')
      .then(m => m.ProductDetailComponent)
},
{ path: 'demo-checkout',
  loadComponent: () => import('./features/demo-checkout/demo-checkout.component')
    .then(m => m.DemoCheckoutComponent) },
{ path: 'account/orders', loadComponent: () => import('./features/account/orders/my-orders.component').then(m => m.MyOrdersComponent) },



  {
    path: 'checkout/success',
    loadComponent: () =>
      import('./features/checkout/success/checkout-success.component')
        .then(m => m.CheckoutSuccessComponent)
  },
  {
    path: 'checkout/cancel',
    loadComponent: () =>
      import('./features/checkout/cancel/checkout-cancel.component')
        .then(m => m.CheckoutCancelComponent)
  },

  // Mis pedidos
  {
    path: 'account/orders',
    loadComponent: () =>
      import('./features/account/orders/my-orders.component')
        .then(m => m.MyOrdersComponent)
  },

  // ⛳️ Siempre al final
  { path: '**', redirectTo: 'catalog' }
];
