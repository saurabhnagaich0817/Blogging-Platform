import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { MediaNewsletterService } from '../../core/services/media-newsletter.service';
import { Notification } from '../../core/models/models';

@Component({
  selector: 'app-navbar',
  template: `
    <header class="navbar" [class.scrolled]="isScrolled">
      <div class="nav-container app-container">
        <!-- Logo -->
        <a routerLink="/" class="logo">
          <mat-icon class="logo-icon">history_edu</mat-icon>
          <span class="logo-text">InkWell</span>
        </a>

        <!-- Desktop Navigation -->
        <nav class="desktop-nav">
          <a [routerLink]="canCreatePost ? '/posts/my' : '/explore'" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}">Stories</a>
          <a routerLink="/explore" routerLinkActive="active">Explore</a>
          <a routerLink="/digest" routerLinkActive="active">Newsletter</a>
          <a routerLink="/community" routerLinkActive="active" *ngIf="isAdmin">Community</a>
        </nav>

        <!-- Right Side Actions -->
        <div class="nav-actions">
          
          <!-- Not Logged In -->
          <ng-container *ngIf="!isLoggedIn">
            <a routerLink="/auth/login" class="nav-link-btn">Sign In</a>
            <a routerLink="/auth/register" class="btn-premium sm">Get Started</a>
          </ng-container>

          <!-- Logged In -->
          <ng-container *ngIf="isLoggedIn">
            
            <!-- Write Post Button (Only Authors & Admins) -->
            <a routerLink="/posts/create" class="btn-premium outline sm" *ngIf="canCreatePost">
              <mat-icon style="font-size: 18px; width: 18px; height: 18px;">edit_square</mat-icon>
              Write
            </a>

            <!-- Notifications -->
            <button mat-icon-button class="icon-btn-premium sm notification-btn" [matMenuTriggerFor]="notificationMenu">
              <mat-icon>notifications</mat-icon>
              <span class="badge" *ngIf="unreadCount > 0">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
            </button>
            
            <mat-menu #notificationMenu="matMenu" xPosition="before" class="premium-menu notifications-menu">
              <div class="menu-header">
                <h3>Notifications</h3>
                <button class="text-btn text-amber" *ngIf="unreadCount > 0" (click)="markAllAsRead($event)">Mark all read</button>
              </div>
              <div class="divider-amber"></div>
              
              <div class="notifications-list" (click)="$event.stopPropagation()">
                <div class="notification-empty" *ngIf="!notifications || notifications.length === 0">
                  <mat-icon>notifications_paused</mat-icon>
                  <p>You're all caught up!</p>
                </div>
                
                <ng-container *ngIf="notifications && notifications.length > 0">
                  <div class="notification-item" *ngFor="let n of notifications" [class.unread]="!n.isRead" (click)="onNotificationClick(n)">
                    <div class="n-icon">
                      <mat-icon *ngIf="n.type === 'Like'">favorite</mat-icon>
                      <mat-icon *ngIf="n.type === 'Comment'">chat_bubble</mat-icon>
                      <mat-icon *ngIf="n.type === 'System'">info</mat-icon>
                      <mat-icon *ngIf="n.type === 'Social'">handshake</mat-icon>
                    </div>
                    <div class="n-content">
                      <div style="display: flex; justify-content: space-between; align-items: flex-start;">
                        <strong>{{ n.title || 'Notification' }}</strong>
                        <button class="delete-notif-btn" (click)="deleteNotification($event, n)" title="Delete notification">
                          <mat-icon style="font-size: 14px; width: 14px; height: 14px; opacity: 0.5;">close</mat-icon>
                        </button>
                      </div>
                      <p>{{ n.message || 'New activity on your profile.' }}</p>
                      
                      <!-- Action Buttons for Requests -->
                      <div class="n-actions" *ngIf="isRequest(n)" (click)="$event.stopPropagation()">
                        <button class="btn-action approve" (click)="approveRequest(n)">
                          <mat-icon>check</mat-icon> Approve
                        </button>
                        <button class="btn-action reject" (click)="rejectRequest(n)">
                          <mat-icon>close</mat-icon> Reject
                        </button>
                      </div>

                      <span class="n-time">{{ n.createdAt | date:'short' }}</span>
                    </div>
                    <div class="unread-dot" *ngIf="!n.isRead"></div>
                  </div>
                </ng-container>
              </div>
              <div class="divider-amber"></div>
              <div class="menu-footer" style="padding: 12px; text-align: center;">
                <a routerLink="/dashboard" class="view-all" style="color: var(--text-secondary); text-decoration: none; font-size: 0.85rem; font-weight: 600;">View Dashboard</a>
              </div>
            </mat-menu>
            
            <div class="nav-divider"></div>

            <!-- User Menu -->
            <button mat-icon-button class="user-avatar-btn" [matMenuTriggerFor]="userMenu">
              <img *ngIf="userPhoto" [src]="userPhoto" class="avatar-img-sm" [alt]="userName">
              <div *ngIf="!userPhoto" class="avatar-sm">{{ getInitial() }}</div>
            </button>
            
            <mat-menu #userMenu="matMenu" xPosition="before" class="premium-menu user-dropdown">
              <div class="user-header">
                <strong style="display: block; font-size: 1rem; color: var(--text-primary);">{{ userName || 'User' }}</strong>
                <span class="role-badge" style="display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.7rem; font-weight: 800; text-transform: uppercase; background: rgba(245,158,11,0.15); color: var(--primary); margin-top: 4px;">{{ userRole || 'Reader' }}</span>
                <span class="email" style="display: block; font-size: 0.8rem; color: var(--text-muted); margin-top: 4px;">{{ userEmail }}</span>
              </div>
              <div class="divider"></div>
              
              <a mat-menu-item routerLink="/dashboard">
                <mat-icon>dashboard</mat-icon> Dashboard
              </a>
              <a mat-menu-item routerLink="/posts/my" *ngIf="canCreatePost">
                <mat-icon>article</mat-icon> My Stories
              </a>
              <a mat-menu-item routerLink="/posts/saved">
                <mat-icon>bookmark</mat-icon> Saved Posts
              </a>
              <a mat-menu-item routerLink="/profile/{{ userName }}">
                <mat-icon>person</mat-icon> Public Profile
              </a>
              
              <div class="divider"></div>
              <a mat-menu-item routerLink="/admin" *ngIf="isAdmin" class="admin-link">
                <mat-icon>admin_panel_settings</mat-icon> Platform Admin
              </a>
              <div class="divider" *ngIf="isAdmin"></div>
              
              <button mat-menu-item (click)="logout()" class="logout-btn">
                <mat-icon>logout</mat-icon> Sign Out
              </button>
            </mat-menu>
            
          </ng-container>

          <!-- Mobile Menu Toggle -->
          <button class="icon-btn-premium sm mobile-toggle" (click)="mobileMenuOpen = !mobileMenuOpen">
            <mat-icon>{{ mobileMenuOpen ? 'close' : 'menu' }}</mat-icon>
          </button>
        </div>
      </div>
      
      <!-- Mobile Nav Dropdown -->
      <div class="mobile-nav" [class.open]="mobileMenuOpen">
        <a [routerLink]="canCreatePost ? '/posts/my' : '/explore'" routerLinkActive="active" [routerLinkActiveOptions]="{exact: true}" (click)="mobileMenuOpen = false">Stories</a>
        <a routerLink="/explore" routerLinkActive="active" (click)="mobileMenuOpen = false">Explore</a>
        <a routerLink="/digest" routerLinkActive="active" (click)="mobileMenuOpen = false">Newsletter</a>
        <a routerLink="/community" routerLinkActive="active" (click)="mobileMenuOpen = false">Community</a>
        <a routerLink="/posts/create" *ngIf="isLoggedIn && canCreatePost" class="text-amber" (click)="mobileMenuOpen = false">Write a Story</a>
      </div>
    </header>
  `,
  styles: [`
    .navbar {
      position: fixed; top: 0; left: 0; right: 0;
      height: 72px; z-index: 1000;
      background: rgba(8, 8, 16, 0.6);
      backdrop-filter: blur(12px); -webkit-backdrop-filter: blur(12px);
      border-bottom: 1px solid transparent;
      transition: all 0.3s ease;
    }
    .navbar.scrolled {
      background: rgba(13, 13, 24, 0.9);
      border-bottom-color: var(--glass-border);
      box-shadow: 0 4px 20px rgba(0,0,0,0.5);
    }
    
    .nav-container { display: flex; justify-content: space-between; align-items: center; height: 100%; }
    
    .logo {
      display: flex; align-items: center; gap: 10px; text-decoration: none;
      font-family: 'Playfair Display', serif; font-size: 1.6rem; font-weight: 800; color: var(--text-primary);
    }
    .logo-icon { color: var(--primary); font-size: 28px; width: 28px; height: 28px; }
    
    .desktop-nav { display: flex; gap: 32px; align-items: center; }
    .desktop-nav a {
      text-decoration: none; color: var(--text-secondary); font-size: 0.95rem; font-weight: 600;
      transition: color 0.2s; position: relative;
    }
    .desktop-nav a:hover { color: var(--text-primary); }
    .desktop-nav a.active { color: var(--primary); }
    .desktop-nav a.active::after {
      content: ''; position: absolute; bottom: -24px; left: 0; right: 0;
      height: 3px; background: var(--primary); border-radius: 3px 3px 0 0;
    }
    
    .nav-actions { 
      display: flex; align-items: center; gap: 12px; flex-shrink: 0; 
    }
    .nav-divider { width: 1px; height: 24px; background: var(--glass-border-subtle); margin: 0 4px; }
    
    @media (max-width: 1200px) {
      .desktop-nav { gap: 16px; }
      .nav-actions { gap: 8px; }
    }
    
    @media (max-width: 1050px) {
      .btn-premium.outline.sm span { display: none; }
      .btn-premium.outline.sm { width: 40px; height: 40px; border-radius: 50%; padding: 0; display: flex; align-items: center; justify-content: center; }
      .desktop-nav a { font-size: 0.85rem; }
    }
    
    @media (max-width: 900px) {
      .desktop-nav { display: none; }
      .nav-divider { display: none; }
    }
    
    .avatar-img-sm { width: 32px; height: 32px; border-radius: 50%; object-fit: cover; border: 2px solid transparent; transition: border-color 0.2s; }
    .user-avatar-btn:hover .avatar-img-sm { border-color: var(--primary); }
    
    .nav-link-btn {
      text-decoration: none; color: var(--text-primary); font-size: 0.95rem; font-weight: 600; padding: 8px 16px;
    }
    .nav-link-btn:hover { color: var(--primary); }
    
    .notification-btn { position: relative; border: none; background: transparent; }
    .badge {
      position: absolute; top: -2px; right: -2px; background: var(--accent); color: white;
      font-size: 0.7rem; font-weight: 800; min-width: 18px; height: 18px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center; padding: 0;
      box-shadow: 0 0 10px var(--accent-glow); border: 2px solid var(--bg-card);
      z-index: 10;
    }
    
    .user-avatar-btn { border: none; background: transparent; cursor: pointer; padding: 0; border-radius: 50%; }
    .user-avatar-btn .avatar-sm { border: 2px solid transparent; transition: border-color 0.2s; }
    .user-avatar-btn:hover .avatar-sm { border-color: var(--primary); }
    
    .mobile-toggle { display: none; }
    .mobile-nav { display: none; }
    
    /* Premium Menus are now handled globally in styles.css */

    
    .user-header { padding: 16px 20px; }
    .user-header strong { display: block; font-size: 1rem; color: var(--text-primary); }
    .user-header .email { display: block; font-size: 0.8rem; color: var(--text-muted); margin-top: 4px; }
    .role-badge {
      display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 0.7rem; font-weight: 800; text-transform: uppercase;
      background: rgba(245,158,11,0.15); color: var(--primary); margin-top: 4px;
    }
    
    .logout-btn { color: var(--accent) !important; }
    .logout-btn:hover { background: rgba(239,68,68,0.08) !important; color: #f87171 !important; }
    .admin-link { color: var(--primary) !important; font-weight: 700; }
    
    /* Notifications Menu */
    .notifications-menu { min-width: 320px; max-width: 380px; }
    .menu-header { display: flex; justify-content: space-between; align-items: center; padding: 14px 20px; }
    .menu-header h3 { margin: 0; font-family: 'Inter', sans-serif; font-size: 1rem; font-weight: 700; }
    .text-btn { background: none; border: none; font-size: 0.8rem; font-weight: 600; cursor: pointer; padding: 0; }
    .text-btn:hover { text-decoration: underline; }
    
    .notifications-list { max-height: 340px; overflow-y: auto; }
    .notification-empty { padding: 40px 20px; text-align: center; color: var(--text-muted); }
    .notification-empty mat-icon { font-size: 36px; width: 36px; height: 36px; margin-bottom: 12px; opacity: 0.5; }
    
    .notification-item {
      display: flex; gap: 14px; padding: 16px 20px; cursor: pointer; transition: background 0.2s; position: relative;
    }
    .notification-item:hover { background: var(--bg-hover); }
    .notification-item.unread { background: rgba(245,158,11,0.03); }
    .n-icon {
      width: 32px; height: 32px; border-radius: 50%; background: rgba(255,255,255,0.05);
      display: flex; align-items: center; justify-content: center; color: var(--text-secondary); flex-shrink: 0;
    }
    .notification-item.unread .n-icon { background: rgba(245,158,11,0.15); color: var(--primary); }
    .n-content { flex: 1; }
    .n-content strong { display: block; font-size: 0.9rem; color: var(--text-primary); margin-bottom: 2px; }
    .n-content p { margin: 0 0 6px; font-size: 0.85rem; color: var(--text-secondary); line-height: 1.4; }
    .n-time { font-size: 0.75rem; color: var(--text-muted); }
    .unread-dot {
      width: 8px; height: 8px; border-radius: 50%; background: var(--primary); position: absolute; top: 28px; right: 16px;
      box-shadow: 0 0 8px var(--primary-glow);
    }
    
    .view-all:hover { color: var(--primary); }

    .n-actions { display: flex; gap: 8px; margin-top: 10px; margin-bottom: 8px; }
    .btn-action {
      display: flex; align-items: center; gap: 4px; padding: 4px 10px; border-radius: 6px;
      font-size: 0.75rem; font-weight: 700; cursor: pointer; border: 1px solid transparent;
      transition: all 0.2s;
    }
    .btn-action mat-icon { font-size: 14px; width: 14px; height: 14px; }
    .btn-action.approve { background: rgba(34,197,94,0.1); color: #4ade80; border-color: rgba(34,197,94,0.2); }
    .btn-action.approve:hover { background: rgba(34,197,94,0.2); }
    .btn-action.reject { background: rgba(239,68,68,0.1); color: #f87171; border-color: rgba(239,68,68,0.2); }
    .btn-action.reject:hover { background: rgba(239,68,68,0.2); }
    
    @media (max-width: 900px) {
      .desktop-nav { display: none; }
      .hide-mobile { display: none; }
      .mobile-toggle { display: flex; }
      .mobile-nav {
        display: flex; flex-direction: column; position: fixed; top: 72px; left: 0; right: 0;
        background: var(--bg-card); border-bottom: 1px solid var(--glass-border);
        padding: 10px 0; transform: translateY(-100%); opacity: 0; transition: all 0.3s ease;
        pointer-events: none; z-index: 990;
      }
      .mobile-nav.open { transform: translateY(0); opacity: 1; pointer-events: auto; }
      .mobile-nav a { padding: 16px 24px; color: var(--text-primary); text-decoration: none; font-weight: 600; border-bottom: 1px solid var(--glass-border-subtle); }
      .mobile-nav a:last-child { border-bottom: none; }
    }
  `]
})
export class NavbarComponent implements OnInit, OnDestroy {
  isLoggedIn = false;
  userEmail = '';
  userName = '';
  userRole = '';
  userPhoto = '';
  canCreatePost = false;
  isAdmin = false;
  
  notifications: Notification[] = [];
  unreadCount = 0;
  
  isScrolled = false;
  mobileMenuOpen = false;
  private scrollListener: () => void;

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService,
    private newsletterService: MediaNewsletterService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.scrollListener = () => {
      this.isScrolled = window.scrollY > 20;
    };
  }

  ngOnInit(): void {
    // Page scroll listen karte hain taaki navbar ka glass effect change kar sakein
    window.addEventListener('scroll', this.scrollListener);
    
    // AuthService se current user ka state subscribe karte hain
    this.authService.currentUser$.subscribe(user => {
      this.isLoggedIn = !!user;
      this.userEmail = user?.email || '';
      this.userName = user?.username || user?.fullName || 'User';
      this.userRole = user?.role || 'Reader';
      this.userPhoto = user?.profilePictureUrl || '';
      
      // Role-based logic: Sirf Author aur Admin hi post create kar sakte hain
      this.canCreatePost = this.userRole === 'Author' || this.userRole === 'Admin';
      this.isAdmin = this.userRole === 'Admin';

      // Agar user login hai, toh notifications load karo aur real-time check chalu karo
      if (this.isLoggedIn) {
        this.loadNotifications();
        // 🚀 Polling Start: Har 5-30 second mein naye notifications check karne ke liye
        this.startPolling();
      } else {
        // Logout hone par polling band kar dete hain memory bachane ke liye
        this.stopPolling();
      }
    });
  }

  private pollingSubscription?: any;
  
  /// <summary>
  /// Ye method background mein periodic checks chalu rakhta hai.
  /// </summary>
  startPolling(): void {
    if (this.pollingSubscription) return; // Dubara start na ho jaye
    
    // Abhi testing ke liye 5 seconds rakha hai, production mein isey 30s-60s kar sakte hain
    const intervalTime = 5000; 
    this.pollingSubscription = setInterval(() => {
      if (this.isLoggedIn) {
        this.loadNotifications();
      }
    }, intervalTime);
  }

  stopPolling(): void {
    if (this.pollingSubscription) {
      clearInterval(this.pollingSubscription);
      this.pollingSubscription = undefined;
    }
  }

  /// <summary>
  /// Backend API se notifications fetch karne ka logic.
  /// </summary>
  loadNotifications(): void {
    this.notificationService.getNotifications().subscribe({
      next: (res) => {
        this.notifications = res.data || [];
        // Unread count calculate karte hain badge dikhane ke liye
        this.unreadCount = this.notifications.filter(n => !n.isRead).length;
      }
    });
  }

  ngOnDestroy(): void {
    // Memory leaks se bachne ke liye listeners aur intervals hta dete hain
    window.removeEventListener('scroll', this.scrollListener);
    this.stopPolling();
  }

  getInitial(): string {
    return this.userName ? this.userName.charAt(0).toUpperCase() : 'U';
  }

  onNotificationClick(notification: Notification): void {
    this.markAsRead(notification);
    
    if (notification.type === 'Like' || notification.type === 'Comment' || notification.type === 'Post') {
      if (notification.relatedId) {
        this.router.navigate(['/posts', notification.relatedId]);
      }
    } else if (this.isRequest(notification)) {
      // Stay on menu to allow approve/reject
    }
  }

  isRequest(n: Notification): boolean {
    const title = n.title.toLowerCase();
    // Only show buttons if it's a NEW request and NOT a status update like 'Sent', 'Approved', etc.
    const isNewRequest = (title.includes('request') || title.includes('subscription')) && 
                         !title.includes('sent') && 
                         !title.includes('approved') && 
                         !title.includes('rejected') &&
                         !title.includes('accepted');
    return isNewRequest;
  }

  approveRequest(n: Notification): void {
    if (n.title.includes('Upgrade')) {
      const refData = n.relatedId?.split(':');
      if (refData && refData.length === 2) {
        const targetUserId = refData[0];
        const role = refData[1];
        this.authService.approveUpgrade(targetUserId, role).subscribe({
          next: () => {
            this.toastr.success('User upgraded to ' + role + '. Tell them to Relogin.');
            this.removeNotification(n);
          },
          error: () => this.toastr.error('Failed to approve upgrade')
        });
        return;
      }
    }
    
    if (n.title.includes('Connection')) {
      const requesterId = n.relatedId;
      if (requesterId) {
        this.authService.acceptConnection(requesterId).subscribe({
          next: (res) => {
            this.toastr.success(res.message || 'Connection accepted!');
            this.removeNotification(n);
          },
          error: (err) => this.toastr.error(err.error?.message || 'Failed to accept connection')
        });
      }
    } else if (n.title.includes('Newsletter') || n.title.includes('Subscription')) {
      const subId = n.relatedId;
      if (subId) {
        this.newsletterService.approve(subId).subscribe({
          next: () => {
            this.toastr.success('Subscription approved!');
            this.removeNotification(n);
          },
          error: () => this.toastr.error('Failed to approve subscription')
        });
      }
    }
  }

  rejectRequest(n: Notification): void {
    if (n.title.includes('Connection')) {
      const requesterId = n.relatedId;
      if (requesterId) {
        this.authService.rejectConnection(requesterId).subscribe({
          next: () => {
            this.toastr.info('Connection request rejected');
            this.removeNotification(n);
          },
          error: () => this.toastr.error('Failed to reject connection')
        });
        return;
      }
    }
    
    // Default rejection for others: just remove notification
    this.removeNotification(n);
    this.toastr.info('Request removed');
  }

  deleteNotification(event: Event, n: Notification): void {
    event.stopPropagation();
    this.removeNotification(n);
  }

  private removeNotification(n: Notification): void {
    this.notificationService.deleteNotification(n.notificationId).subscribe({
      next: () => {
        this.notifications = this.notifications.filter(notif => notif.notificationId !== n.notificationId);
        this.unreadCount = this.notifications.filter(notif => !notif.isRead).length;
      },
      error: () => {
        // If delete fails, still remove from UI but show warning
        this.notifications = this.notifications.filter(notif => notif.notificationId !== n.notificationId);
      }
    });
  }

  markAsRead(notification: Notification): void {
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.notificationId).subscribe({
        next: () => {
          notification.isRead = true;
          this.unreadCount = Math.max(0, this.unreadCount - 1);
        }
      });
    }
  }

  markAllAsRead(event: Event): void {
    event.stopPropagation();
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
      }
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
