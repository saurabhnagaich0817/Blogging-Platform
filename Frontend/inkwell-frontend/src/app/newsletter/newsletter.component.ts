/* 
 UI TEMPLATE STRUCTURE:
 1. Page container
 2. Header (icon + title)
 3. Subscription Form
 4. Admin Panel (Approve/Reject)
 5. Glassmorphism Design
 6. Toast feedback
*/

import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { MediaNewsletterService } from '../core/services/media-newsletter.service';
import { AuthService } from '../core/services/auth.service';
import { Subscriber, BaseResponse } from '../core/models/models';

@Component({
  selector: 'app-newsletter',
  template: `
    <div class="app-container py-60">
      <header class="newsletter-header animate-in">
        <div class="badge-premium">Community</div>
        <h1 class="heading-xl">The <span class="text-gradient">InkWell</span> Digest</h1>
        <p class="text-secondary subtitle-xl">Join 50,000+ creators and get the best stories in your inbox.</p>
      </header>

      <div class="newsletter-grid mt-40">
        <!-- Main Form -->
        <div class="glass-card p-40 sub-hero animate-in">
          <div class="sub-icon-box mb-24">
             <mat-icon>mail_outline</mat-icon>
          </div>
          <h2 class="heading-md">Weekly Perspectives</h2>
          <p class="text-secondary mb-32">Curated stories, technical deep-dives, and exclusive interviews delivered every Tuesday.</p>

          <form [formGroup]="newsletterForm" (ngSubmit)="onSubmit()" class="sub-form-premium">
            <div class="input-group">
               <label class="premium-label">Full Name</label>
               <div class="premium-input-box">
                  <mat-icon>person_outline</mat-icon>
                  <input type="text" formControlName="fullName" placeholder="John Doe">
               </div>
            </div>

            <div class="input-group mt-20">
               <label class="premium-label">Email Address</label>
               <div class="premium-input-box">
                  <mat-icon>alternate_email</mat-icon>
                  <input type="email" formControlName="email" placeholder="john@example.com">
               </div>
            </div>

            <button type="submit" class="btn-premium full-width mt-32" [disabled]="newsletterForm.invalid || loading">
               <mat-spinner *ngIf="loading" diameter="20" color="accent"></mat-spinner>
               <span *ngIf="!loading">Join the community</span>
               <mat-icon *ngIf="!loading">bolt</mat-icon>
            </button>
          </form>
          <p class="privacy-note mt-24">We respect your privacy. Unsubscribe anytime with one click.</p>
        </div>

        <!-- Admin View -->
        <div class="admin-view animate-in" style="animation-delay: 0.2s" *ngIf="isAdmin">
          <div class="glass-card p-32">
             <div class="admin-header mb-24">
                <h3 class="heading-sm">Pending Approvals</h3>
                <span class="count-pill">{{ subscribers.length }}</span>
             </div>

             <div class="subscribers-list">
                <div *ngFor="let sub of subscribers" class="sub-node">
                   <div class="node-info">
                      <span class="node-name">{{ sub.fullName }}</span>
                      <span class="node-email">{{ sub.email }}</span>
                      <span class="status-pill" [class]="sub.status.toLowerCase()">{{ sub.status }}</span>
                   </div>
                   <div class="node-actions" *ngIf="sub.status === 'Pending'">
                      <button class="icon-btn-node success" (click)="approve(sub)">
                         <mat-icon>check</mat-icon>
                      </button>
                      <button class="icon-btn-node danger" (click)="reject(sub)">
                         <mat-icon>close</mat-icon>
                      </button>
                   </div>
                </div>
                
                <div *ngIf="subscribers.length === 0" class="empty-node">
                   <mat-icon>done_all</mat-icon>
                   <p>All caught up!</p>
                </div>
             </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .py-60 { padding-top: 60px; padding-bottom: 60px; }
    .mt-40 { margin-top: 40px; }
    .mt-32 { margin-top: 32px; }
    .mt-24 { margin-top: 24px; }
    .mt-20 { margin-top: 20px; }
    .mb-32 { margin-bottom: 32px; }
    .mb-24 { margin-bottom: 24px; }
    .p-40 { padding: 40px; }
    .p-32 { padding: 32px; }

    .newsletter-header { text-align: center; border-bottom: 1px solid var(--glass-border); padding-bottom: 40px; }
    .badge-premium { display: inline-block; padding: 4px 12px; background: rgba(99, 102, 241, 0.1); color: var(--primary); border-radius: 20px; font-size: 0.8rem; font-weight: 700; text-transform: uppercase; margin-bottom: 12px; }
    .subtitle-xl { font-size: 1.25rem; margin-top: 12px; }

    .newsletter-grid { display: grid; grid-template-columns: 1fr; gap: 40px; max-width: 1000px; margin-inline: auto; }
    
    .sub-hero { text-align: center; }
    .sub-icon-box { width: 64px; height: 64px; background: var(--grad-main); border-radius: 20px; display: flex; align-items: center; justify-content: center; margin: 0 auto; color: white; }
    .sub-icon-box mat-icon { font-size: 32px; width: 32px; height: 32px; }

    .sub-form-premium { max-width: 400px; margin: 0 auto; text-align: left; }
    .privacy-note { font-size: 0.85rem; color: var(--text-muted); }

    .admin-header { display: flex; justify-content: space-between; align-items: center; }
    .count-pill { padding: 2px 10px; background: var(--primary); color: white; border-radius: 20px; font-size: 0.75rem; font-weight: 700; }

    .subscribers-list { display: flex; flex-direction: column; gap: 12px; }
    .sub-node { display: flex; justify-content: space-between; align-items: center; padding: 16px; background: rgba(255,255,255,0.02); border-radius: 12px; border: 1px solid var(--glass-border); }
    .node-info { display: flex; flex-direction: column; }
    .node-name { font-weight: 700; font-size: 0.95rem; }
    .node-email { font-size: 0.85rem; color: var(--text-muted); }
    
    .status-pill { display: inline-block; width: fit-content; padding: 2px 8px; border-radius: 6px; font-size: 0.7rem; font-weight: 800; text-transform: uppercase; margin-top: 4px; }
    .status-pill.pending { background: rgba(245, 158, 11, 0.1); color: #f59e0b; }
    .status-pill.active { background: rgba(34, 197, 94, 0.1); color: #22c55e; }
    .status-pill.rejected { background: rgba(239, 68, 68, 0.1); color: #ef4444; }

    .node-actions { display: flex; gap: 8px; }
    .icon-btn-node { width: 32px; height: 32px; border-radius: 8px; border: 1px solid var(--glass-border); background: none; cursor: pointer; transition: all 0.3s; display: flex; align-items: center; justify-content: center; }
    .icon-btn-node mat-icon { font-size: 18px; width: 18px; height: 18px; }
    .icon-btn-node.success:hover { border-color: #22c55e; color: #22c55e; }
    .icon-btn-node.danger:hover { border-color: #ef4444; color: #ef4444; }

    .empty-node { text-align: center; padding: 40px; color: var(--text-muted); }
    .empty-node mat-icon { font-size: 40px; width: 40px; height: 40px; opacity: 0.3; margin-bottom: 12px; }

    @media (min-width: 1000px) {
      .newsletter-grid { grid-template-columns: 1fr 400px; }
      .newsletter-header { text-align: left; }
    }
  `]
})
export class NewsletterComponent implements OnInit {
  newsletterForm: FormGroup;
  loading = false;
  subscribers: Subscriber[] = [];
  isAdmin = false;

  constructor(
    private fb: FormBuilder,
    private newsletterService: MediaNewsletterService,
    private authService: AuthService,
    private toastr: ToastrService
  ) {
    this.newsletterForm = this.fb.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.isAdmin = user?.role === 'Admin';
    if (this.isAdmin) this.loadSubscribers();
  }

  loadSubscribers(): void {
    this.newsletterService.getAllSubscribers().subscribe((res: BaseResponse<Subscriber[]>) => {
      this.subscribers = res.data || [];
    });
  }

  onSubmit(): void {
    if (this.newsletterForm.invalid) return;

    this.loading = true;
    const user = this.authService.getCurrentUser();
    
    this.newsletterService.subscribe({
      ...this.newsletterForm.value,
      userId: user?.id
    }).subscribe({
      next: (res: BaseResponse<string>) => {
        this.loading = false;
        this.toastr.success(res.message, 'Success');
        this.newsletterForm.reset();
        if (this.isAdmin) this.loadSubscribers();
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Subscription failed. Please try again.', 'Error');
      }
    });
  }

  approve(sub: Subscriber): void {
    this.newsletterService.approve(sub.subscriberId).subscribe((res: BaseResponse<string>) => {
      sub.status = 'Active';
      this.toastr.success(`${sub.fullName} approved!`, 'Admin');
    });
  }

  reject(sub: Subscriber): void {
    this.newsletterService.reject(sub.subscriberId).subscribe((res: BaseResponse<string>) => {
      sub.status = 'Rejected';
      this.toastr.warning(`${sub.fullName} rejected.`, 'Admin');
    });
  }
}
