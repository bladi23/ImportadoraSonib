import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';


@Injectable({ providedIn: 'root' })
export class AdminGuard implements CanActivate {
constructor(private router: Router) {}
canActivate(): boolean {
const roles = (window as any).rolesCache || [];
const token = localStorage.getItem('token');
// Fallback: leer bandera de roles que guardaremos tras /me (ver app.component)
if (!token || !roles.includes('Admin')) { this.router.navigate(['/login']); return false; }
return true;
}
}