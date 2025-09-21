import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('token');
  const authReq = req.clone({
    withCredentials: true, // 👈 manda cookie de sesión en todas las peticiones
    setHeaders: token ? { Authorization: `Bearer ${token}` } : {}
  });
  return next(authReq);
};
