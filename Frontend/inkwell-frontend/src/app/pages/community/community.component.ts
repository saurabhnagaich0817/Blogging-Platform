import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { User, BaseResponse } from '../../core/models/models';

@Component({
  selector: 'app-community',
  template: `
    <div class="app-container py-60 animate-in">
      <div class="community-header mb-60 text-center">
        <span class="badge-amber mb-16" style="display:inline-flex">Global Network</span>
        <h1 class="heading-xl">The <span class="text-gradient">InkWell</span> Community</h1>
        <p class="text-secondary subtitle-xl">Connect with authors and readers building the future of writing.</p>
      </div>

      <!-- Loading Skeletons -->
      <div class="users-grid" *ngIf="loading">
        <div class="glass-card user-skeleton" *ngFor="let i of [1,2,3,4,5,6]">
          <div class="skeleton-shimmer"></div>
        </div>
      </div>

      <!-- Error -->
      <div class="glass-card state-card mt-40 animate-in" *ngIf="!loading && errorMessage">
        <mat-icon>group_off</mat-icon>
        <h2>Could not load community</h2>
        <p>{{ errorMessage }}</p>
        <button class="btn-premium outline mt-24" (click)="loadUsers()">Try Again</button>
      </div>

      <!-- User Grid -->
      <div class="users-grid" *ngIf="!loading && !errorMessage && users.length > 0">
        <div class="glass-card user-card animate-in" *ngFor="let user of users; let i = index" [style.animation-delay]="(i * 0.06) + 's'" [routerLink]="['/profile', user.username]" style="cursor: pointer;">
          <div class="user-avatar-ring">
            <div class="user-avatar">{{ getInitial(user) }}</div>
          </div>

          <div class="user-info">
            <h3 class="user-name">{{ user.fullName || user.username || 'InkWell Member' }}</h3>
            <span class="username-text" *ngIf="user.username">&#64;{{ user.username }}</span>
            <div class="mt-8">
              <span class="badge-amber" *ngIf="user.role === 'Admin'">Admin</span>
              <span class="badge-blue" *ngIf="user.role === 'Author'">Author</span>
              <span class="badge-green" *ngIf="user.role === 'Reader'">Reader</span>
            </div>
          </div>

          <p class="user-bio text-secondary mt-16">Member of the InkWell community since {{ (user.createdAt | date:'yyyy') || 'recently' }}.</p>
        </div>
      </div>

      <!-- Empty State -->
      <div class="glass-card state-card mt-40 animate-in" *ngIf="!loading && !errorMessage && users.length === 0">
        <mat-icon>groups</mat-icon>
        <h2>Community is just getting started!</h2>
        <p>No users found yet. Be the first to join the conversation.</p>
        <button class="btn-premium outline mt-24" routerLink="/auth/register">Join InkWell</button>
      </div>
    </div>
  `,
  styles: [`
    .py-60 { padding-top: 60px; padding-bottom: 60px; }
    .mb-60 { margin-bottom: 60px; }
    .mt-40 { margin-top: 40px; }
    .mt-16 { margin-top: 16px; }
    .mb-16 { margin-bottom: 16px; }
    .mt-8 { margin-top: 8px; }

    .subtitle-xl { max-width: 620px; margin: 12px auto 0; font-size: 1.1rem; }

    .users-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 28px; }

    .user-card { padding: 40px 24px; text-align: center; display: flex; flex-direction: column; align-items: center; gap: 12px; transition: transform 0.3s ease, border-color 0.3s; }
    .user-card:hover { transform: translateY(-4px); border-color: var(--primary); }

    .user-avatar-ring {
      width: 90px; height: 90px; border-radius: 50%; padding: 4px;
      background: var(--grad-main); margin-bottom: 8px;
    }
    .user-avatar {
      width: 100%; height: 100%; border-radius: 50%; background: var(--bg-deep);
      display: flex; align-items: center; justify-content: center;
      font-size: 2rem; font-weight: 800; color: white; border: 4px solid var(--bg-deep);
    }

    .user-info { display: flex; flex-direction: column; align-items: center; gap: 4px; }
    .user-name { margin: 0; font-size: 1.2rem; font-weight: 800; color: var(--text-primary); font-family: 'Inter', sans-serif; }
    .username-text { font-size: 0.9rem; color: var(--text-muted); font-weight: 600; }

    .user-bio { font-size: 0.9rem; margin: 0; line-height: 1.5; }

    .state-card { padding: 48px 36px; text-align: center; }
    .state-card mat-icon { width: 64px; height: 64px; font-size: 64px; color: var(--primary); opacity: 0.9; margin-bottom: 12px; }
    .state-card h2 { margin: 0 0 12px; }

    /* Skeleton */
    .user-skeleton { min-height: 300px; position: relative; overflow: hidden; }
    .skeleton-shimmer { position: absolute; inset: 0; background: linear-gradient(90deg, transparent, rgba(255,255,255,0.03), transparent); animation: shimmer 1.5s infinite; }
  `]
})
export class CommunityComponent implements OnInit {
  users: User[] = [];
  loading = false;
  errorMessage = '';

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';

    this.authService.getAllUsers().subscribe({
      next: (res: BaseResponse<User[]>) => {
        this.loading = false;
        this.users = res.data || [];
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'Could not load community members.';
      }
    });
  }

  getInitial(user: User): string {
    const name = user.fullName || user.username || user.email || '?';
    return name.trim().charAt(0).toUpperCase();
  }
}
