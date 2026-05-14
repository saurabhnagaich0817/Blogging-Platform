import { Injectable } from '@angular/core';
import { HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Injectable()
export class JwtInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    const isApiUrl = request.url.includes('/api/');

    if (token && isApiUrl) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        const isAuthEndpoint = request.url.includes('/api/auth/');
        const isNotifEndpoint = request.url.includes('/api/notifications');

        switch (error.status) {
          case 401:
            if (!isAuthEndpoint) {
              this.toastr.warning('Session expired. Please sign in again.', 'Session Expired');
              this.authService.logout();
              this.router.navigate(['/auth/login'], { queryParams: { reason: 'session_expired' } });
            }
            break;

          case 403:
            this.toastr.error('You do not have the required permissions.', 'Access Denied');
            break;

          case 500:
            if (!isNotifEndpoint) {
              this.toastr.error('Server error occurred. Please try again.', 'Server Error');
            }
            break;

          case 0:
            if (!isNotifEndpoint) {
              this.toastr.error('Cannot connect to server. Are the services running?', 'Connection Error');
            }
            break;

          default:
            if (!isNotifEndpoint) {
              const errorMsg = error.error?.message || error.message || 'An unexpected error occurred';
              this.toastr.error(errorMsg, `Error ${error.status}`);
            }
            break;
        }

        return throwError(() => error);
      })
    );
  }
}
