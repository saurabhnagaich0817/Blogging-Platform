import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CategoryService } from '../core/services/category.service';
import { TagService } from '../core/services/tag.service';
import { MediaNewsletterService } from '../core/services/media-newsletter.service';
import { AuthService } from '../core/services/auth.service';
import { BaseResponse, Category, Tag, Subscriber, User } from '../core/models/models';

@Component({
  selector: 'app-admin',
  template: `
    <div class="app-container py-60">
      <header class="section-header mb-40 animate-in">
        <div class="badge-amber mb-16" style="display:inline-flex"><mat-icon style="font-size:14px; width:14px; height:14px; margin-right:4px">admin_panel_settings</mat-icon> Platform Admin</div>
        <h1 class="heading-xl">Mission <span class="text-gradient">Control.</span></h1>
        <p class="text-secondary">Manage platform architecture, subscribers, and community metadata.</p>
      </header>

      <div class="admin-grid animate-in" style="animation-delay: 0.1s">
        <!-- Sidebar Navigation -->
        <aside class="admin-nav-wrapper">
          <nav class="admin-nav glass-card">
            <button class="nav-btn" [class.active]="activeTab === 'categories'" (click)="activeTab = 'categories'">
              <mat-icon>folder</mat-icon> Categories
            </button>
            <button class="nav-btn" [class.active]="activeTab === 'tags'" (click)="activeTab = 'tags'">
              <mat-icon>label</mat-icon> Tags
            </button>
            <button class="nav-btn" [class.active]="activeTab === 'newsletter'" (click)="activeTab = 'newsletter'">
              <mat-icon>mark_email_read</mat-icon> Subscribers
            </button>
            <button class="nav-btn" [class.active]="activeTab === 'users'" (click)="activeTab = 'users'">
              <mat-icon>people</mat-icon> Users
            </button>
          </nav>
        </aside>

        <!-- Main Content Area -->
        <main class="admin-content">
          
          <!-- Categories Tab -->
          <div *ngIf="activeTab === 'categories'" class="glass-card p-32 animate-in">
            <div class="flex-between mb-24">
              <h2 class="heading-md">Category Management</h2>
            </div>
            
            <form [formGroup]="categoryForm" (ngSubmit)="createCategory()" class="flex gap-16 mb-32 border-bottom pb-32">
              <div class="input-group" style="flex: 1">
                <input type="text" class="premium-input-box" style="width: 100%; color: white; background: rgba(255,255,255,0.05)" formControlName="name" placeholder="New category name (e.g., Technology)">
              </div>
              <button type="submit" class="btn-premium sm" [disabled]="categoryForm.invalid">Add Category</button>
            </form>

            <div class="table-responsive">
              <table class="premium-table full-width">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Slug</th>
                    <th class="text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngIf="categories.length === 0">
                    <td colspan="3" class="text-center text-secondary py-32" style="color: rgba(255,255,255,0.5)">No categories found.</td>
                  </tr>
                  <tr *ngFor="let cat of categories">
                    <td><strong style="color: white">{{ cat.name }}</strong></td>
                    <td style="color: var(--text-muted)">{{ cat.slug }}</td>
                    <td class="text-right">
                      <button class="icon-btn-premium sm danger" (click)="deleteCategory(cat.categoryId)"><mat-icon>delete</mat-icon></button>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Tags Tab -->
          <div *ngIf="activeTab === 'tags'" class="glass-card p-32 animate-in">
            <h2 class="heading-md mb-24">Tag Library</h2>
            
            <form [formGroup]="tagForm" (ngSubmit)="createTag()" class="flex gap-16 mb-32 border-bottom pb-32">
              <div class="input-group" style="flex: 1">
                <input type="text" class="premium-input-box" style="width: 100%; color: white; background: rgba(255,255,255,0.05)" formControlName="name" placeholder="New tag name (e.g., angular)">
              </div>
              <button type="submit" class="btn-premium sm" [disabled]="tagForm.invalid">Create Tag</button>
            </form>

            <div class="tags-container">
              <div class="text-center text-secondary py-20 full-width" *ngIf="tags.length === 0" style="color: rgba(255,255,255,0.3)">
                No tags created yet.
              </div>
              <div class="tag-pill" *ngFor="let tag of tags">
                <mat-icon style="font-size:14px; width:14px; height:14px; margin-right:4px; opacity:0.5">tag</mat-icon>
                <span style="color: white">{{ tag.name }}</span>
                <span class="count">{{ tag.postCount || 0 }}</span>
                <button class="tag-del" (click)="deleteTag(tag.tagId)"><mat-icon>close</mat-icon></button>
              </div>
            </div>
          </div>

          <!-- Newsletter Tab -->
          <div *ngIf="activeTab === 'newsletter'" class="glass-card p-32 animate-in">
            <h2 class="heading-md mb-24">Newsletter Audience</h2>

            <div class="table-responsive">
              <table class="premium-table full-width">
                <thead>
                  <tr>
                    <th>Subscriber</th>
                    <th>Email</th>
                    <th>Status</th>
                    <th class="text-right">Moderate</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngIf="subscribers.length === 0">
                    <td colspan="4" class="text-center text-secondary py-32" style="color: rgba(255,255,255,0.5)">No subscribers yet.</td>
                  </tr>
                  <tr *ngFor="let sub of subscribers">
                    <td><strong style="color: white">{{ sub.fullName || 'Anonymous' }}</strong></td>
                    <td style="color: var(--text-muted)">{{ sub.email }}</td>
                    <td>
                      <span class="badge-amber" *ngIf="isPending(sub.status)">Pending</span>
                      <span class="badge-green" *ngIf="sub.status?.toLowerCase() === 'active'">Active</span>
                      <span class="badge-red" *ngIf="sub.status?.toLowerCase() === 'rejected'">Rejected</span>
                    </td>
                    <td class="text-right">
                      <div class="flex gap-8" style="justify-content: flex-end" *ngIf="isPending(sub.status)">
                        <button class="icon-btn-premium sm" (click)="moderateSubscriber(sub.subscriberId, 'approve')" title="Approve"><mat-icon style="color: #22c55e">check</mat-icon></button>
                        <button class="icon-btn-premium sm danger" (click)="moderateSubscriber(sub.subscriberId, 'reject')" title="Reject"><mat-icon>close</mat-icon></button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

          <!-- Users Tab -->
          <div *ngIf="activeTab === 'users'" class="glass-card p-32 animate-in">
            <h2 class="heading-md mb-24">Platform Directory</h2>

            <div class="table-responsive">
              <table class="premium-table full-width">
                <thead>
                  <tr>
                    <th>User</th>
                    <th>Email</th>
                    <th>Role</th>
                    <th class="text-right">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  <tr *ngIf="users.length === 0">
                    <td colspan="4" class="text-center text-secondary py-32">Loading users...</td>
                  </tr>
                  <tr *ngFor="let user of users">
                    <td><strong style="color: white">{{ user.fullName || user.username }}</strong></td>
                    <td style="color: var(--text-muted)">{{ user.email }}</td>
                    <td>
                      <span class="badge-amber" *ngIf="user.role === 'Admin'">{{ user.role }}</span>
                      <span class="badge-blue" *ngIf="user.role === 'Author'">{{ user.role }}</span>
                      <span class="badge-green" *ngIf="user.role === 'Reader'">{{ user.role }}</span>
                    </td>
                    <td class="actions text-right">
                      <div class="flex gap-8 justify-end">
                        <button class="btn-premium sm ghost text-primary" *ngIf="user.role !== 'Admin'" (click)="approveUpgrade(user.userId || user.id, 'Admin')">Make Admin</button>
                        <button class="btn-premium sm ghost text-blue" *ngIf="user.role !== 'Author'" (click)="approveUpgrade(user.userId || user.id, 'Author')">Make Author</button>
                        <button class="btn-premium sm ghost text-green" *ngIf="user.role !== 'Reader'" (click)="approveUpgrade(user.userId || user.id, 'Reader')">Make Reader</button>
                        <button class="icon-btn-premium sm danger" (click)="deleteUser(user.userId || user.id)" title="Delete User"><mat-icon>delete</mat-icon></button>
                      </div>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>

        </main>
      </div>
    </div>
  `,
  styles: [`
    .admin-grid { display: grid; grid-template-columns: 240px minmax(0, 1fr); gap: 32px; align-items: start; }
    
    .admin-nav { display: flex; flex-direction: column; padding: 12px; gap: 8px; }
    .nav-btn {
      display: flex; align-items: center; gap: 12px; padding: 12px 16px; border: none; background: transparent;
      color: rgba(255,255,255,0.5); font-family: 'Inter', sans-serif; font-size: 0.95rem; font-weight: 600;
      border-radius: var(--radius-md); cursor: pointer; transition: all 0.2s; text-align: left;
    }
    .nav-btn:hover { background: rgba(255,255,255,0.05); color: var(--text-primary); }
    .nav-btn.active { background: rgba(245,158,11,0.1); color: var(--primary); }
    .nav-btn mat-icon { font-size: 20px; width: 20px; height: 20px; }

    .border-bottom { border-bottom: 1px solid rgba(255,255,255,0.08); }
    .pb-32 { padding-bottom: 32px; }

    .premium-table { border-collapse: collapse; text-align: left; }
    .premium-table th { padding: 16px 20px; background: rgba(255,255,255,0.03); font-size: 0.75rem; font-weight: 700; color: rgba(255,255,255,0.4); text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid rgba(255,255,255,0.08); }
    .premium-table td { padding: 16px 20px; border-bottom: 1px solid rgba(255,255,255,0.05); vertical-align: middle; font-size: 0.95rem; }
    .premium-table tr:last-child td { border-bottom: none; }
    .premium-table tr:hover { background: rgba(255,255,255,0.01); }
    .text-right { text-align: right; }

    .tags-container { display: flex; flex-wrap: wrap; gap: 12px; }
    .tag-pill {
      display: inline-flex; align-items: center; padding: 6px 6px 6px 12px; border-radius: 99px;
      background: rgba(255,255,255,0.05); border: 1px solid rgba(255,255,255,0.1); font-size: 0.85rem; font-weight: 600;
      transition: all 0.2s;
    }
    .tag-pill:hover { background: rgba(255,255,255,0.08); border-color: var(--primary); }
    .tag-pill .count { margin-left: 8px; padding: 2px 6px; border-radius: 10px; background: rgba(0,0,0,0.3); font-size: 0.7rem; color: var(--primary); font-weight: 700; }
    .tag-del { background: none; border: none; padding: 2px; margin-left: 6px; color: rgba(255,255,255,0.3); cursor: pointer; border-radius: 50%; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
    .tag-del:hover { background: rgba(239,68,68,0.2); color: #ef4444; }
    .tag-del mat-icon { font-size: 14px; width: 14px; height: 14px; }

    .danger { color: #ef4444 !important; }
    .danger:hover { background: rgba(239,68,68,0.1) !important; border-color: #ef4444 !important; }

    .full-width { width: 100%; }

    @media (max-width: 900px) {
      .admin-grid { grid-template-columns: 1fr; }
      .admin-nav { flex-direction: row; flex-wrap: wrap; }
      .nav-btn { flex: 1; min-width: 140px; justify-content: center; }
    }
  `]
})
export class AdminComponent implements OnInit {
  activeTab: 'categories' | 'tags' | 'newsletter' | 'users' = 'categories';
  
  categories: Category[] = [];
  tags: Tag[] = [];
  subscribers: Subscriber[] = [];
  users: User[] = [];

  categoryForm: FormGroup;
  tagForm: FormGroup;

  constructor(
    private categoryService: CategoryService,
    private tagService: TagService,
    private newsletterService: MediaNewsletterService,
    private authService: AuthService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {
    this.categoryForm = this.fb.group({ name: ['', Validators.required] });
    this.tagForm = this.fb.group({ name: ['', Validators.required] });
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.categoryService.getAllCategories().subscribe((res: any) => this.categories = res.data || []);
    this.tagService.getAllTags().subscribe((res: any) => this.tags = res.data || []);
    this.newsletterService.getAllSubscribers().subscribe((res: any) => this.subscribers = res.data || []);
    this.authService.getAllUsers().subscribe((res: any) => this.users = res.data || []);
  }

  createCategory(): void {
    if (this.categoryForm.invalid) return;
    this.categoryService.createCategory(this.categoryForm.value).subscribe({
      next: (res: any) => {
        if (res.data) this.categories.push(res.data);
        this.categoryForm.reset();
        this.toastr.success('Category created.');
      }
    });
  }

  deleteCategory(id: string): void {
    if (!confirm('Delete this category?')) return;
    this.categoryService.deleteCategory(id).subscribe({
      next: () => {
        this.categories = this.categories.filter(c => c.categoryId !== id);
        this.toastr.success('Category deleted.');
      }
    });
  }

  createTag(): void {
    if (this.tagForm.invalid) return;
    this.tagService.createTag(this.tagForm.value).subscribe({
      next: (res: any) => {
        if (res.data) this.tags.push(res.data);
        this.tagForm.reset();
        this.toastr.success('Tag created.');
      }
    });
  }

  deleteTag(id: string): void {
    if (!confirm('Delete this tag?')) return;
    this.tagService.deleteTag(id).subscribe({
      next: () => {
        this.tags = this.tags.filter(t => t.tagId !== id);
        this.toastr.success('Tag deleted.');
      }
    });
  }

  deleteUser(id: string): void {
    if(confirm('Are you sure you want to remove this user? This will remove all their data.')) {
      this.authService.deleteUser(id).subscribe({
        next: (res: any) => {
          if (res.success) {
            this.toastr.success('User deleted successfully.');
            this.loadData();
          } else {
            this.toastr.error(res.message || 'Deletion failed.');
          }
        },
        error: (err) => this.toastr.error(err.error?.message || 'Could not delete user.')
      });
    }
  }

  approveUpgrade(userId: string, role: string): void {
    this.authService.approveUpgrade(userId, role).subscribe({
      next: (res: any) => {
        if (res.success) {
          this.toastr.success(`User successfully upgraded to ${role}.`);
          this.loadData(); // reload users
        } else {
          this.toastr.error(res.message || 'Upgrade failed.');
        }
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Could not approve upgrade.');
      }
    });
  }

  isPending(status: string | undefined | null): boolean {
    if (!status) return true;
    const s = status.toLowerCase();
    return s === 'pending' || s === 'pendingapproval' || s === 'waiting';
  }

  moderateSubscriber(id: string, action: 'approve' | 'reject'): void {
    const apiCall = action === 'approve' ? this.newsletterService.approve(id) : this.newsletterService.reject(id);
    apiCall.subscribe({
      next: () => {
        const sub = this.subscribers.find(s => s.subscriberId === id);
        if (sub) sub.status = action === 'approve' ? 'Active' : 'Rejected';
        this.toastr.success(`Subscriber ${action}d.`);
      }
    });
  }
}
