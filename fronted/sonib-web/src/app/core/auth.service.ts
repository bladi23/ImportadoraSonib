import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, EMPTY, Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface AuthUser {
  email: string;
  roles: string[];
}

export interface LoginRes {
  token: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private base = environment.apiBase;
  private _user$ = new BehaviorSubject<AuthUser | null>(null);
  user$ = this._user$.asObservable();

  constructor(private http: HttpClient) {
    this.restoreSession();
  }

  private restoreSession() {
    const token = localStorage.getItem('token');
    if (!token) return;
    // opcional: validar expiración del token aquí
    this.me().subscribe(); // carga email/roles
  }

  currentUser() {
    return this._user$.getValue();
  }

  login(email: string, password: string): Observable<LoginRes> {
    return this.http.post<LoginRes>(`${this.base}/auth/login`, { email, password }).pipe(
      tap(res => {
        localStorage.setItem('token', res.token);
        this._user$.next({ email: res.email, roles: res.roles ?? [] });
      })
    );
  }
 
  afterLogin() {
   return this.me();
  }

  logout() {
    localStorage.removeItem('token');
    this._user$.next(null);
  }

  me() {
    return this.http.get<{ email: string | null; userId: string | null; roles: string[] }>(`${this.base}/auth/me`)
      .pipe(
        tap(m => {
          if (m?.email) this._user$.next({ email: m.email, roles: m.roles ?? [] });
          else this._user$.next(null);
        }),
        catchError(() => { this._user$.next(null); return of(null); })
      );
  }

  // Opcional: endpoints de recuperación de contraseña si ya tienes en backend
  forgot(email: string) {
    return this.http.post(`${this.base}/auth/forgot`, { email })
      .pipe(catchError(err => { throw err; }));
  }

  reset(code: string, email: string, newPassword: string) {
    return this.http.post(`${this.base}/auth/reset`, { code, email, newPassword })
      .pipe(catchError(err => { throw err; }));
  }
  register(email: string, password: string) {
  return this.http.post(`${this.base}/auth/register`, { email, password });
}

}
