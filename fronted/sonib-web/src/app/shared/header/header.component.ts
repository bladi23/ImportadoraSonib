import { Component, inject, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth.service';
import { LoginPanelComponent } from '../login-panel/login-panel.component';
import { CartService } from '../../core/cart.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, LoginPanelComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent implements OnInit {
  // inyecciones con `inject` (no uses parámetros en constructor)
  private auth = inject(AuthService);
  private router = inject(Router);
  private cart = inject(CartService);

  user$ = this.auth.user$;
  count$ = this.cart.count$;

  showLogin = false;

  ngOnInit() {
    // carga el contador al iniciar
    this.cart.refresh();
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
