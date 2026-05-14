import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  template: `
    <div class="auth-page">
      <!-- Left visual panel -->
      <div class="auth-visual">
        <div class="visual-glow"></div>
        <div class="visual-body">
          <div class="visual-logo">
            <mat-icon>auto_stories</mat-icon>
            <span>InkWell</span>
          </div>
          <h1 class="heading-xl">Every great story<br><span class="text-gradient">starts here.</span></h1>
          <p class="text-secondary mt-20">
            InkWell is where writers, thinkers, and readers come together to share ideas that matter.
          </p>

          <div class="role-preview mt-40">
            <div class="role-preview-card" [class.active]="selectedRole === 'Reader'">
              <div class="role-icon reader-icon"><mat-icon>auto_stories</mat-icon></div>
              <div>
                <strong>Reader</strong>
                <p>Read, like, save, and comment on stories. The best way to start.</p>
              </div>
            </div>
            <div class="role-preview-card" [class.active]="selectedRole === 'Author'">
              <div class="role-icon author-icon"><mat-icon>edit_square</mat-icon></div>
              <div>
                <strong>Author</strong>
                <p>Publish stories, create tags, and build your audience on InkWell.</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right form panel -->
      <div class="auth-form-panel">
        <div class="auth-form-card animate-in">
          <div class="mb-32">
            <div class="badge-amber mb-20">Create Account</div>
            <h2 class="heading-lg">Join InkWell</h2>
            <p class="text-secondary">Fill in the details below to get started.</p>
          </div>

          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <!-- Role Selection -->
            <div class="role-toggle mb-32">
              <button type="button" class="role-btn" [class.active]="f['role'].value === 'Reader'" (click)="setRole('Reader')">
                <mat-icon>auto_stories</mat-icon>
                <span>Reader</span>
              </button>
              <button type="button" class="role-btn" [class.active]="f['role'].value === 'Author'" (click)="setRole('Author')">
                <mat-icon>edit_square</mat-icon>
                <span>Author</span>
              </button>
            </div>

            <div class="form-grid-2">
              <div class="input-group">
                <label class="premium-label">Full Name</label>
                <div class="premium-input-box">
                  <mat-icon>person_outline</mat-icon>
                  <input type="text" formControlName="fullName" placeholder="John Doe">
                </div>
                <span class="error-msg" *ngIf="f['fullName'].touched && f['fullName'].invalid">Full name required.</span>
              </div>

              <div class="input-group">
                <label class="premium-label">Username</label>
                <div class="premium-input-box">
                  <mat-icon>alternate_email</mat-icon>
                  <input type="text" formControlName="username" placeholder="johndoe">
                </div>
                <span class="error-msg" *ngIf="f['username'].touched && f['username'].invalid">Username required.</span>
              </div>
            </div>

            <div class="input-group mt-20">
              <label class="premium-label">Email Address</label>
              <div class="premium-input-box">
                <mat-icon>email</mat-icon>
                <input type="email" formControlName="email" placeholder="john@example.com">
              </div>
              <span class="error-msg" *ngIf="f['email'].touched && f['email'].invalid">Valid email required.</span>
            </div>

            <div class="input-group mt-20">
              <label class="premium-label">Password</label>
              <div class="premium-input-box">
                <mat-icon>lock_outline</mat-icon>
                <input [type]="hidePassword ? 'password' : 'text'" formControlName="password" placeholder="At least 6 characters">
                <button type="button" class="visibility-toggle" (click)="hidePassword = !hidePassword">
                  <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
              </div>
              <span class="error-msg" *ngIf="f['password'].touched && f['password'].hasError('required')">Password required.</span>
              <span class="error-msg" *ngIf="f['password'].touched && f['password'].hasError('minlength')">At least 6 characters.</span>
            </div>

            <!-- Role info note -->
            <div class="role-note mt-20" [class.author]="f['role'].value === 'Author'">
              <mat-icon>{{ f['role'].value === 'Author' ? 'edit_square' : 'auto_stories' }}</mat-icon>
              <span>
                <strong>{{ f['role'].value }}</strong> —
                {{ f['role'].value === 'Author' ? 'You can write and publish posts after signing up.' : 'You can read, like, comment, and save posts.' }}
              </span>
            </div>

            <button type="submit" class="btn-premium full-width mt-32" [disabled]="registerForm.invalid || loading">
              <mat-spinner *ngIf="loading" diameter="20"></mat-spinner>
              <span *ngIf="!loading">Create My Account</span>
              <mat-icon *ngIf="!loading">arrow_forward</mat-icon>
            </button>
          </form>

          <div class="auth-footer mt-32">
            Already have an account?
            <a routerLink="/auth/login" class="amber-link">Sign In</a>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      min-height: 100vh;
      display: grid;
      grid-template-columns: 1fr 520px;
      background: var(--bg-deep);
    }

    /* Visual Side */
    .auth-visual {
      position: relative;
      overflow: hidden;
      padding: 60px;
      background: #060610;
      display: flex; align-items: center;
    }
    .visual-glow {
      position: absolute;
      width: 700px; height: 700px;
      background: radial-gradient(circle, rgba(245,158,11,0.12) 0%, transparent 70%);
      top: -200px; left: -200px;
    }
    .visual-body { position: relative; z-index: 2; max-width: 520px; }
    .visual-logo {
      display: flex; align-items: center; gap: 12px;
      font-family: 'Playfair Display', serif;
      font-size: 1.8rem; font-weight: 800;
      color: var(--primary);
      margin-bottom: 48px;
    }
    .visual-logo mat-icon { font-size: 2rem; width: 2rem; height: 2rem; }

    .role-preview { display: flex; flex-direction: column; gap: 16px; }
    .role-preview-card {
      display: flex; align-items: flex-start; gap: 16px;
      padding: 18px 20px; border-radius: 16px;
      border: 1px solid rgba(255,255,255,0.06);
      background: rgba(255,255,255,0.02);
      transition: all 0.3s;
    }
    .role-preview-card.active {
      border-color: rgba(245,158,11,0.3);
      background: rgba(245,158,11,0.05);
    }
    .role-preview-card strong { display: block; font-size: 0.95rem; margin-bottom: 4px; color: var(--text-primary); }
    .role-preview-card p { margin: 0; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.5; }
    .role-icon {
      width: 40px; height: 40px; flex-shrink: 0; border-radius: 12px;
      display: flex; align-items: center; justify-content: center;
    }
    .role-icon mat-icon { font-size: 20px; width: 20px; height: 20px; }
    .reader-icon { background: rgba(245,158,11,0.12); color: var(--primary); }
    .author-icon { background: rgba(99,102,241,0.12); color: #818cf8; }

    /* Form Side */
    .auth-form-panel {
      display: flex; align-items: center; justify-content: center;
      padding: 40px 48px;
      background: var(--bg-surface);
      border-left: 1px solid var(--glass-border);
    }
    .auth-form-card { width: 100%; max-width: 400px; }

    .mb-32 { margin-bottom: 32px; }
    .mb-20 { margin-bottom: 20px; }
    .mt-40 { margin-top: 40px; }
    .mt-32 { margin-top: 32px; }
    .mt-20 { margin-top: 20px; }

    /* Role Toggle */
    .role-toggle {
      display: grid; grid-template-columns: 1fr 1fr; gap: 10px;
    }
    .role-btn {
      display: flex; align-items: center; justify-content: center; gap: 8px;
      padding: 12px 16px; border-radius: var(--radius-md);
      border: 1px solid var(--glass-border-subtle);
      background: rgba(255,255,255,0.02);
      color: var(--text-secondary);
      font-family: 'Inter', sans-serif; font-size: 0.9rem; font-weight: 700;
      cursor: pointer; transition: all 0.25s ease;
    }
    .role-btn mat-icon { font-size: 18px; width: 18px; height: 18px; }
    .role-btn.active {
      border-color: var(--primary);
      background: rgba(245,158,11,0.08);
      color: var(--primary);
    }
    .role-btn:hover:not(.active) { border-color: rgba(255,255,255,0.15); color: var(--text-primary); }

    .form-grid-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }

    .role-note {
      display: flex; align-items: flex-start; gap: 12px;
      padding: 14px 16px; border-radius: var(--radius-md);
      background: rgba(245,158,11,0.06);
      border: 1px solid rgba(245,158,11,0.15);
      color: var(--text-secondary); font-size: 0.88rem; line-height: 1.5;
    }
    .role-note mat-icon { font-size: 18px; width: 18px; height: 18px; color: var(--primary); flex-shrink: 0; margin-top: 2px; }
    .role-note strong { color: var(--primary); }
    .role-note.author { background: rgba(99,102,241,0.06); border-color: rgba(99,102,241,0.2); }
    .role-note.author mat-icon, .role-note.author strong { color: #818cf8; }

    .auth-footer { text-align: center; color: var(--text-muted); font-size: 0.92rem; }
    .amber-link { color: var(--primary); font-weight: 700; text-decoration: none; margin-left: 6px; }
    .amber-link:hover { text-decoration: underline; }

    @media (max-width: 1024px) {
      .auth-page { grid-template-columns: 1fr; }
      .auth-visual { display: none; }
      .auth-form-panel { border-left: none; padding: 40px 24px; }
      .form-grid-2 { grid-template-columns: 1fr; }
    }
  `]
})
export class RegisterComponent {
  registerForm: FormGroup;
  loading = false;
  hidePassword = true;
  selectedRole = 'Reader';

  get f() { return this.registerForm.controls; }

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.registerForm = this.fb.group({
      fullName: ['', Validators.required],
      username: ['', [Validators.required, Validators.minLength(3)]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      role: ['Reader', Validators.required]
    });

    if (this.authService.isLoggedIn()) this.router.navigate(['/dashboard']);
  }

  setRole(role: 'Reader' | 'Author'): void {
    this.selectedRole = role;
    this.registerForm.patchValue({ role });
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const { fullName, username, email, password, role } = this.registerForm.value;

    this.authService.register(username, email, password, role, fullName).subscribe({
      next: (res) => {
        this.loading = false;
        if (res.success) {
          this.toastr.success(
            `Welcome to InkWell! You've joined as ${role}. Please sign in.`,
            '🎉 Account Created'
          );
          this.router.navigate(['/auth/login']);
        } else {
          this.toastr.error(res.message || 'Registration failed.', 'Error');
        }
      },
      error: (err) => {
        this.loading = false;
        this.toastr.error(err.error?.message || 'Unable to register. Please try again.', 'Registration Failed');
      }
    });
  }
}
