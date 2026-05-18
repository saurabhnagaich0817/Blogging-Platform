import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { BaseResponse, AuthResponse, User, UserProfile } from '../models/models';
import { jwtDecode } from 'jwt-decode';

/**
 * Payload structure for the JWT token used in InkWell.
 */
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

/**
 * Service responsible for managing user authentication, identity, and profile operations.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'inkwell_token';
  private currentUserSubject = new BehaviorSubject<User | null>(this.getUserFromToken());
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  /**
   * Registers a new user account.
   */
  register(
    username: string,
    email: string,
    password: string,
    role: string = 'Reader',
    fullName: string = ''
  ): Observable<BaseResponse<AuthResponse>> {
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/register`,
      { username, email, password, role, fullName }
    );
  }

  /**
   * Authenticates a user and stores the issued JWT.
   */
  login(email: string, password: string): Observable<BaseResponse<AuthResponse>> {
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/login`,
      { email, password }
    ).pipe(
      tap((response: BaseResponse<AuthResponse>) => {
        if (response.success && response.data?.token) {
          this.setToken(response.data.token);
        }
      })
    );
  }

  /**
   * Authenticates a user using Google OAuth.
   */
  googleLogin(googleToken: string): Observable<BaseResponse<AuthResponse>> {
    return this.http.post<BaseResponse<AuthResponse>>(
      `${environment.apiUrl}/auth/google-login`,
      { token: googleToken }
    ).pipe(
      tap((response: BaseResponse<AuthResponse>) => {
        if (response.success && response.data?.token) {
          this.setToken(response.data.token);
        }
      })
    );
  }

  /**
   * Clears the current session and navigates to the login page.
   */
  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    this.currentUserSubject.next(null);
    this.router.navigate(['/auth/login']);
  }

  /**
   * Manually sets the authentication token and updates user state.
   */
  setToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
    this.currentUserSubject.next(this.getUserFromToken());
  }

  /**
   * Retrieves the current JWT from local storage.
   */
  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  /**
   * Checks if a user session is active.
   */
  isLoggedIn(): boolean {
    return !!this.getUserFromToken();
  }

  /**
   * Returns the currently authenticated user object.
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  /**
   * Returns the role of the current user. Defaults to 'Reader'.
   */
  getUserRole(): string {
    return this.getCurrentUser()?.role ?? 'Reader';
  }

  /**
   * Checks if the user has a specific role.
   */
  hasRole(role: string): boolean {
    return this.getUserRole() === role;
  }

  /**
   * Fetches all platform users (Admin view).
   */
  getAllUsers(): Observable<BaseResponse<User[]>> {
    return this.http.get<BaseResponse<User[]>>(`${environment.apiUrl}/users`);
  }

  /**
   * Fetches public profile details for a specific user.
   */
  getProfile(username: string): Observable<BaseResponse<User>> {
    return this.http.get<BaseResponse<User>>(`${environment.apiUrl}/users/profile/${username}`);
  }

  /**
   * Requests a role upgrade to the backend.
   */
  requestUpgrade(role: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/request-upgrade`, { role });
  }

  /**
   * Approves a user's role upgrade (Admin operation).
   */
  approveUpgrade(userId: string, role: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${userId}/approve-upgrade`, { role });
  }

  /**
   * Updates the profile picture URL for the current user.
   */
  updateProfilePicture(url: string): Observable<BaseResponse<string>> {
    return this.http.patch<BaseResponse<string>>(`${environment.apiUrl}/users/profile-picture`, { url });
  }

  /**
   * Updates the authenticated user's profile details.
   */
  updateProfile(profile: UserProfile): Observable<BaseResponse<string>> {
    const dto = {
      displayName: profile.fullName || profile.displayName,
      bio: profile.bio,
      phoneNumber: profile.phoneNumber,
      linkedInUrl: profile.linkedInUrl,
      githubUrl: profile.githubUrl
    };
    return this.http.patch<BaseResponse<string>>(`${environment.apiUrl}/users/profile`, dto);
  }

  /**
   * Sends a connection request to another user.
   */
  requestConnection(targetUserId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${targetUserId}/connect`, {});
  }

  /**
   * Accepts a pending connection request.
   */
  acceptConnection(requesterId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${requesterId}/accept-connect`, {});
  }

  /**
   * Rejects a pending connection request.
   */
  rejectConnection(requesterId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${environment.apiUrl}/users/${requesterId}/reject-connect`, {});
  }

  /**
   * Retrieves the current connection status with another user.
   */
  getConnectionStatus(targetUserId: string): Observable<BaseResponse<string>> {
    return this.http.get<BaseResponse<string>>(`${environment.apiUrl}/users/${targetUserId}/connection-status`);
  }

  /**
   * Fetches pending connection requests for the current user.
   */
  getPendingConnections(): Observable<BaseResponse<User[]>> {
    return this.http.get<BaseResponse<User[]>>(`${environment.apiUrl}/users/pending-connections`);
  }

  /**
   * Deletes a user account (Admin only).
   */
  deleteUser(userId: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${environment.apiUrl}/users/${userId}`);
  }

  /**
   * Decodes the JWT and constructs a User object.
   */
  private getUserFromToken(): User | null {
    try {
      const token = localStorage.getItem(this.TOKEN_KEY);
      if (!token) return null;

      const decoded = jwtDecode<JwtPayload>(token);
      if (!decoded || decoded.exp * 1000 < Date.now()) {
        localStorage.removeItem(this.TOKEN_KEY);
        return null;
      }

      // Map standard and OIDC claim types to local User model
      const roleClaim = decoded['role'] ?? decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const id = decoded['userId'] ?? decoded['nameid'] ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ?? decoded['sub'] ?? decoded['id'] ?? '';
      const email = decoded['email'] ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ?? '';
      const username = decoded['username'] ?? decoded['unique_name'] ?? (email ? email.split('@')[0] : '');
      const fullName = decoded['name'] ?? decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? '';
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
      console.warn('Authentication token could not be parsed.', error);
      localStorage.removeItem(this.TOKEN_KEY);
      return null;
    }
  }
}
