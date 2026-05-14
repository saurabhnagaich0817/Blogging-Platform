import { Component, AfterViewInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

declare const google: any;

@Component({
  selector: 'app-login',
  template: `
    <div class="auth-page">
      <!-- Right visual panel (reversed for login) -->
      <div class="auth-visual login-visual">
        <div class="visual-glow amber"></div>
        <div class="visual-body">
          <h1 class="heading-xl">Welcome back to<br><span class="text-gradient">InkWell.</span></h1>
          <p class="text-secondary mt-20" style="font-size: 1.1rem; line-height: 1.6;">
            Sign in to continue reading premium stories, interact with the community, or publish your next masterpiece.
          </p>

          <div class="stats-row mt-40">
            <div class="stat-item">
              <span class="stat-val text-gradient">50k+</span>
              <span class="stat-label">Active Readers</span>
            </div>
            <div class="stat-item">
              <span class="stat-val text-gradient">12k+</span>
              <span class="stat-label">Stories Published</span>
            </div>
            <div class="stat-item">
              <span class="stat-val text-gradient">99%</span>
              <span class="stat-label">Quality Content</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Left form panel -->
      <div class="auth-form-panel">
        <div class="auth-form-card animate-in">
          <div class="mobile-logo">
            <mat-icon>auto_stories</mat-icon> InkWell
          </div>

          <div class="mb-40">
            <h2 class="heading-lg">Sign In</h2>
            <p class="text-secondary">Enter your email and password to access your account.</p>
          </div>

          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="auth-stack">
            <div class="input-group">
              <label class="premium-label">Email Address</label>
              <div class="premium-input-box">
                <mat-icon>email</mat-icon>
                <input type="email" formControlName="email" placeholder="john@example.com">
              </div>
              <span class="error-msg" *ngIf="f['email'].touched && f['email'].invalid">Valid email required.</span>
            </div>

            <div class="input-group mt-20">
              <div class="flex-between mb-8">
                <label class="premium-label" style="margin: 0">Password</label>
              </div>
              <div class="premium-input-box">
                <mat-icon>lock_outline</mat-icon>
                <input [type]="hidePassword ? 'password' : 'text'" formControlName="password" placeholder="Enter your password">
                <button type="button" class="visibility-toggle" (click)="hidePassword = !hidePassword">
                  <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
              </div>
              <span class="error-msg" *ngIf="f['password'].touched && f['password'].invalid">Password required.</span>
            </div>

            <button type="submit" class="btn-premium full-width mt-32" [disabled]="loginForm.invalid || loading">
              <mat-spinner *ngIf="loading && !googleLoading" diameter="20" style="margin: 0 auto;"></mat-spinner>
              <ng-container *ngIf="!loading || googleLoading">
                <span>Sign In</span>
                <mat-icon style="margin-left: 8px;">login</mat-icon>
              </ng-container>
            </button>
          </form>

          <div class="divider-text mt-32 mb-32"><span>or continue with</span></div>

          <div class="google-btn-container">
            <div id="google-btn-wrapper"></div>
            <div class="google-loader" *ngIf="googleLoading">
              <mat-spinner diameter="24"></mat-spinner>
              <span>Connecting to Google...</span>
            </div>
          </div>

          <div class="auth-footer mt-40">
            New to InkWell?
            <a routerLink="/auth/register" class="amber-link">Create Account</a>
          </div>
        </div>
      </div>

      <!-- Role Selection Overlay -->
      <div class="role-overlay" *ngIf="showRoleSelection">
        <div class="role-card animate-in">
          <div class="badge-amber mb-16">Final Step</div>
          <h2 class="heading-md">Choose your <span class="text-gradient">experience.</span></h2>
          <p class="text-secondary mb-32">How do you plan to use InkWell?</p>
          
          <div class="role-options">
            <div class="role-opt" [class.active]="selectedRole === 'Reader'" (click)="selectedRole = 'Reader'">
              <mat-icon>auto_stories</mat-icon>
              <div>
                <strong>Reader</strong>
                <p>Read, save and interact with stories.</p>
              </div>
            </div>
            <div class="role-opt" [class.active]="selectedRole === 'Author'" (click)="selectedRole = 'Author'">
              <mat-icon>edit_note</mat-icon>
              <div>
                <strong>Author</strong>
                <p>Write, publish and build an audience.</p>
              </div>
            </div>
            <div class="role-opt" [class.active]="selectedRole === 'Admin'" (click)="selectedRole = 'Admin'">
              <mat-icon>admin_panel_settings</mat-icon>
              <div>
                <strong>Admin</strong>
                <p>Manage the platform and moderate content.</p>
              </div>
            </div>
          </div>

          <button class="btn-premium full-width mt-32" [disabled]="!selectedRole || roleLoading" (click)="confirmRole()">
            <mat-spinner *ngIf="roleLoading" diameter="20"></mat-spinner>
            <span *ngIf="!roleLoading">Finish Setup</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: grid;
      grid-template-columns: 520px 1fr; /* Form on left, visual on right */
      background: var(--bg-deep);
    }

    /* Form Side (Left) */
    .auth-form-panel {
      display: flex; align-items: center; justify-content: center;
      padding: 40px 48px;
      background: var(--bg-surface);
      border-right: 1px solid var(--glass-border);
    }
    .auth-form-card { width: 100%; max-width: 400px; position: relative; }

    .mobile-logo {
      display: none; align-items: center; gap: 10px;
      font-family: 'Playfair Display', serif; font-size: 1.5rem; font-weight: 800;
      color: var(--primary); margin-bottom: 32px;
    }
    .mobile-logo mat-icon { font-size: 1.8rem; width: 1.8rem; height: 1.8rem; }

    .forgot-link { font-size: 0.85rem; font-weight: 600; color: var(--text-secondary); text-decoration: none; transition: color 0.2s; }
    .forgot-link:hover { color: var(--primary); }

    .divider-text {
      display: flex; align-items: center; text-align: center; color: var(--text-muted); font-size: 0.85rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em;
    }
    .divider-text::before, .divider-text::after { content: ''; flex: 1; border-bottom: 1px solid var(--glass-border-subtle); }
    .divider-text span { padding: 0 16px; }

    /* Google Button Area */
    .google-btn-container { position: relative; min-height: 44px; display: flex; justify-content: center; }
    .google-loader {
      position: absolute; inset: 0; background: var(--bg-card); border-radius: var(--radius-md); border: 1px solid var(--glass-border-subtle);
      display: flex; align-items: center; justify-content: center; gap: 12px; color: var(--text-secondary); font-size: 0.9rem; font-weight: 600; z-index: 10;
    }

    .auth-footer { text-align: center; color: var(--text-muted); font-size: 0.92rem; }
    .amber-link { color: var(--primary); font-weight: 700; text-decoration: none; margin-left: 6px; }
    .amber-link:hover { text-decoration: underline; }

    /* Visual Side (Right) */
    .auth-visual {
      position: relative; overflow: hidden; padding: 60px;
      background: #060610; display: flex; align-items: center; justify-content: center;
    }
    .visual-glow.amber {
      position: absolute; width: 800px; height: 800px;
      background: radial-gradient(circle, rgba(245,158,11,0.08) 0%, transparent 60%);
      top: 50%; left: 50%; transform: translate(-50%, -50%);
    }
    .visual-body { position: relative; z-index: 2; max-width: 520px; text-align: center; }

    .stats-row {
      display: flex; justify-content: center; gap: 40px;
      padding: 30px; border-radius: 24px;
      background: rgba(255,255,255,0.02); border: 1px solid rgba(255,255,255,0.05);
      backdrop-filter: blur(10px);
    }
    .stat-item { display: flex; flex-direction: column; align-items: center; gap: 4px; }
    .stat-val { font-family: 'Playfair Display', serif; font-size: 2.2rem; font-weight: 800; }
    .stat-label { font-size: 0.8rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; color: var(--text-muted); }

    @media (max-width: 1024px) {
      .auth-page { grid-template-columns: 1fr; }
      .auth-visual { display: none; }
      .auth-form-panel { border-right: none; padding: 40px 24px; }
      .mobile-logo { display: flex; }
    }
    
    .role-overlay {
      position: fixed; inset: 0; background: rgba(8, 8, 16, 0.95);
      backdrop-filter: blur(20px); z-index: 2000;
      display: flex; align-items: center; justify-content: center; padding: 24px;
    }
    .role-card {
      background: var(--bg-card); border: 1px solid var(--glass-border);
      padding: 48px; border-radius: 32px; width: 100%; max-width: 500px;
      box-shadow: 0 20px 80px rgba(0,0,0,0.5); text-align: center;
    }
    .role-options { display: flex; flex-direction: column; gap: 16px; margin-top: 32px; }
    .role-opt {
      display: flex; align-items: center; gap: 20px; padding: 24px;
      background: rgba(255,255,255,0.03); border: 2px solid transparent;
      border-radius: 20px; cursor: pointer; text-align: left; transition: all 0.3s ease;
    }
    .role-opt:hover { background: rgba(255,255,255,0.06); border-color: rgba(245,158,11,0.3); }
    .role-opt.active { background: rgba(245,158,11,0.1); border-color: var(--primary); }
    .role-opt mat-icon { font-size: 32px; width: 32px; height: 32px; color: var(--text-muted); transition: color 0.3s; }
    .role-opt.active mat-icon { color: var(--primary); }
    .role-opt strong { display: block; font-size: 1.1rem; color: var(--text-primary); margin-bottom: 4px; }
    .role-opt p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); }
  `]
})
export class LoginComponent implements AfterViewInit {
  loginForm: FormGroup;
  loading = false;
  googleLoading = false;
  roleLoading = false;
  hidePassword = true;
  
  showRoleSelection = false;
  selectedRole: 'Reader' | 'Author' | 'Admin' | null = null;
  googleToken: string = '';

  get f() { return this.loginForm.controls; }

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });

    if (this.authService.isLoggedIn()) this.router.navigate(['/dashboard']);
  }

  ngAfterViewInit(): void {
    this.initGoogleSignIn();
  }

  private initGoogleSignIn(): void {
    if (typeof google !== 'undefined' && google.accounts) {
      google.accounts.id.initialize({
        client_id: environment.googleClientId,
        callback: this.handleGoogleResponse.bind(this)
      });
      google.accounts.id.renderButton(
        document.getElementById('google-btn-wrapper'),
        { theme: 'outline', size: 'large', type: 'standard', text: 'signin_with', shape: 'rectangular', width: 400 }
      );
    } else {
      setTimeout(() => this.initGoogleSignIn(), 500); // Retry if library not loaded yet
    }
  }

  handleGoogleResponse(response: any): void {
    if (response.credential) {
      this.googleLoading = true;
      this.authService.googleLogin(response.credential).subscribe({
        next: (res) => {
          this.googleLoading = false;
          if (res.success) {
            if (res.data?.isNewUser) {
              this.showRoleSelection = true;
              this.googleToken = response.credential;
              this.toastr.info('Welcome to InkWell! Please choose your role.', 'New Account');
            } else {
              this.toastr.success('Welcome back to InkWell.', 'Login Successful');
              this.router.navigate(['/dashboard']);
            }
          } else {
            this.toastr.error(res.message || 'Google login failed.', 'Error');
          }
        },
        error: (err) => {
          this.googleLoading = false;
          this.toastr.error(err.error?.message || 'Google login failed. Please try again.', 'Error');
        }
      });
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const { email, password } = this.loginForm.value;

    this.authService.login(email, password).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          const role = this.authService.getUserRole();
          this.toastr.success(`Signed in as ${role}.`, 'Welcome Back');
          this.router.navigate(['/dashboard']);
        } else {
          this.toastr.error(res.message || 'Invalid credentials.', 'Login Failed');
        }
      },
      error: (err) => {
        this.loading = false;
        this.toastr.error(err.error?.message || 'Unable to sign in. Check your credentials.', 'Login Failed');
      }
    });
  }

  confirmRole(): void {
    if (!this.selectedRole) return;
    
    this.roleLoading = true;
    // We send a role upgrade request immediately after login
    this.authService.requestUpgrade(this.selectedRole).subscribe({
      next: () => {
        this.roleLoading = false;
        this.toastr.success(`You are now registered as ${this.selectedRole}.`, 'Setup Complete');
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.roleLoading = false;
        // If upgrade fails, we still let them in as Reader
        this.router.navigate(['/dashboard']);
      }
    });
  }
}
