import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, map } from 'rxjs';
import { ApiService } from './api.service';

export interface UserState { email: string; roles: string[]; token: string }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);

  private _user$ = new BehaviorSubject<UserState | null>(null);
  user$ = this._user$.asObservable();
  isLoggedIn$ = this.user$.pipe(map(u => !!u));
  isAdmin$ = this.user$.pipe(map(u => !!u?.roles?.includes('Admin')));

  constructor() {
    const token = localStorage.getItem('token');
    const email = localStorage.getItem('email');
    if (token && email) {
      this.api.me().subscribe({
        next: (me) => this._user$.next({ email: me.email, roles: me.roles, token }),
        error: () => this.logout()
      });
    }
  }

  login(email: string, password: string) { return this.api.login(email, password); }

  afterLogin() {
    const token = localStorage.getItem('token')!;
    this.api.me().subscribe(me => this._user$.next({ email: me.email, roles: me.roles, token }));
  }

  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('email');
    this._user$.next(null);
  }
}
