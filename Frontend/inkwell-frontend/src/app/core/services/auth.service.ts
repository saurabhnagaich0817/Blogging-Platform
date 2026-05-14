import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { BaseResponse, AuthResponse, User, UserProfile } from '../models/models';
import { jwtDecode } from 'jwt-decode';

interface JwtPayload {
  exp: number;
  sub?: string;
  userId?: string;
  nameid?: string;
  id?: string;
  email?: string;
  username?: string;
  name?: string;
  role?: string | string[];
  unique_name?: string;
  [key: string]: any;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'inkwell_token';
  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromToken());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  // This method sends the user's registration data to the Backend API.
  register(
    username: string,
    email: string,
    password: string,
    role: string = 'Reader',
    fullName: string = ''
  ): Observable<BaseResponse<AuthResponse>> {
    // We send a POST request to the /auth/register endpoint.
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/register`,
      { username, email, password, role, fullName }
    );
  }

  // This method is used to log in the user and get a secure token from the Backend.
  login(email: string, password: string): Observable<BaseResponse<AuthResponse>> {
    // We send the email and password to our API Gateway.
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/login`,
      { email, password }
    ).pipe(
      tap((response: any) => {
        // If login is successful, we get a 'token' which is like a digital ID card.
        if ((response.success || response.Success) && response.data?.token) {
          // We save this token in the browser's memory so the user stays logged in.
          this.setToken(response.data.token);
        }
      })
    );
  }

  googleLogin(googleToken: string): Observable<BaseResponse<AuthResponse>> {
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/google-login`,
      { token: googleToken }
    ).pipe(
      tap((response: any) => {
        if ((response.success || response.Success) && response.data?.token) {
          this.setToken(response.data.token);
        }
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/auth/login']);
  }

  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.currentUserSubject.next(this.getUserFromToken());
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return !!this.getUserFromToken();
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  getUserRole(): string {
    return this.getCurrentUser()?.role ?? 'Reader';
  }

  hasRole(role: string): boolean {
    return this.getUserRole() === role;
  }

  getAllUsers(): Observable<BaseResponse<User[]>> {
    return this.http.get<BaseResponse<User[]>>(`${environment.apiUrl}/users`);
  }

  getProfile(username: string): Observable<BaseResponse<User>> {
    return this.http.get<BaseResponse<User>>(`${environment.apiUrl}/users/profile/${username}`);
  }

  requestUpgrade(role: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/request-upgrade`, { role });
  }

  approveUpgrade(userId: string, role: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${userId}/approve-upgrade`, { role });
  }

  updateProfilePicture(url: string): Observable<BaseResponse<string>> {
    return this.http.patch<BaseResponse<string>>(`${environment.apiUrl}/users/profile-picture`, { url });
  }

  updateProfile(profile: UserProfile): Observable<BaseResponse<string>> {
    // Map UserProfile to the DTO expected by backend
    const dto = {
      displayName: profile.fullName || profile.displayName,
      bio: profile.bio,
      phoneNumber: profile.phoneNumber,
      linkedInUrl: profile.linkedInUrl,
      githubUrl: profile.githubUrl
    };
    return this.http.put<BaseResponse<string>>(`${environment.apiUrl}/users/profile`, dto);
  }

  requestConnection(targetUserId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${targetUserId}/connect`, {});
  }

  acceptConnection(requesterId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${requesterId}/accept-connect`, {});
  }

  rejectConnection(requesterId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${requesterId}/reject-connect`, {});
  }

  getConnectionStatus(targetUserId: string): Observable<BaseResponse<string>> {
    return this.http.get<BaseResponse<string>>(`${environment.apiUrl}/users/${targetUserId}/connection-status`);
  }

  getPendingConnections(): Observable<BaseResponse<User[]>> {
    return this.http.get<BaseResponse<User[]>>(`${environment.apiUrl}/users/pending-connections`);
  }

  deleteUser(userId: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${environment.apiUrl}/users/${userId}`);
  }

  private getUserFromToken(): User | null {
    try {
      const token = localStorage.getItem(this.TOKEN_KEY);
      if (!token) {
        return null;
      }

      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded || decoded.exp * 1000 < Date.now()) {
        localStorage.removeItem(this.TOKEN_KEY);
        return null;
      }

      const roleClaim = decoded['role'] ??
        decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const id = decoded['userId'] ??
        decoded['nameid'] ??
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
        decoded['sub'] ??
        decoded['id'] ??
        '';
      const email = decoded['email'] ??
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ??
        '';
      const username = decoded['username'] ??
        decoded['unique_name'] ??
        (email ? email.split('@')[0] : '');
      const fullName = decoded['name'] ??
        decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
        '';
      const profilePictureUrl = decoded['profilePictureUrl'] ?? '';

      if (!id) {
        localStorage.removeItem(this.TOKEN_KEY);
        return null;
      }

      return {
        id,
        email,
        role: Array.isArray(roleClaim) ? roleClaim[0] : (roleClaim || 'Reader'),
        username,
        fullName,
        profilePictureUrl
      };
    } catch (error) {
      console.warn('Invalid token found in storage', error);
      localStorage.removeItem(this.TOKEN_KEY);
      return null;
    }
  }
}
