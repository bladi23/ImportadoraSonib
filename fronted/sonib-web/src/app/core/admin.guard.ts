// src/app/core/admin.guard.ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { map, take } from 'rxjs/operators';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.user$.pipe(
    take(1),
    map((u: any) => {
      const ok = !!u && Array.isArray(u.roles) && u.roles.includes('Admin');
      if (ok) return true;
      router.navigate(['/login']);
      return false;
    })
  );
};
