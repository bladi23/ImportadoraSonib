import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth.service';
import { LoginPanelComponent } from '../login-panel/login-panel.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, LoginPanelComponent],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  // declara 
  private auth = inject(AuthService);
  private router = inject(Router);

  user$ = this.auth.user$;
  showLogin = false;

  logout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
