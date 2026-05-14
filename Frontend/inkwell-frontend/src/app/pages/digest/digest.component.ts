import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { MediaNewsletterService } from '../../core/services/media-newsletter.service';
import { BaseResponse } from '../../core/models/models';

@Component({
  selector: 'app-digest',
  template: `
    <div class="app-container animate-in">
      <div class="digest-header py-60">
        <span class="badge-amber mb-16" style="display:inline-flex">Weekly Newsletter</span>
        <h1 class="heading-xl">InkWell <span class="text-gradient">Digest</span></h1>
        <p class="text-secondary subtitle-xl">Handpicked stories, technical deep-dives, and exclusive interviews delivered every Tuesday.</p>
      </div>

      <div class="digest-layout pb-80">
        <div class="glass-card digest-card animate-in">
          <div class="digest-icon-wrap">
            <mat-icon>auto_stories</mat-icon>
          </div>

          <h2 class="heading-md mt-24">Join 50,000+ Creators</h2>
          <p class="text-secondary mb-32">No spam. Just quality content every week. Unsubscribe anytime with one click.</p>

          <form [formGroup]="digestForm" (ngSubmit)="onSubscribe()" class="digest-form">
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
              <span class="error-msg" *ngIf="digestForm.get('email')?.touched && digestForm.get('email')?.invalid">
                A valid email address is required.
              </span>
            </div>

            <button type="submit" class="btn-premium full-width mt-32" [disabled]="digestForm.invalid || loading">
              <mat-spinner *ngIf="loading" diameter="20" color="accent"></mat-spinner>
              <span *ngIf="!loading">Subscribe to Digest</span>
              <mat-icon *ngIf="!loading">bolt</mat-icon>
            </button>
          </form>

          <p class="privacy-note mt-24">We respect your privacy. Unsubscribe anytime.</p>
        </div>

        <div class="digest-features animate-in" style="animation-delay: 0.15s">
          <div class="feature-card glass-card" *ngFor="let f of features">
            <div class="feature-icon">
              <mat-icon>{{ f.icon }}</mat-icon>
            </div>
            <div>
              <h4>{{ f.title }}</h4>
              <p class="text-secondary">{{ f.description }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .py-60 { padding-top: 60px; padding-bottom: 60px; }
    .pb-80 { padding-bottom: 80px; }
    .mb-16 { margin-bottom: 16px; }

    .digest-header { text-align: center; border-bottom: 1px solid var(--glass-border-subtle); margin-bottom: 40px; }
    .subtitle-xl { max-width: 560px; margin: 12px auto 0; font-size: 1.1rem; }

    .digest-layout {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 40px;
      align-items: start;
      max-width: 1000px;
      margin-inline: auto;
    }

    .digest-card { padding: 48px 40px; text-align: center; }

    .digest-icon-wrap {
      width: 72px; height: 72px;
      background: var(--grad-main); border-radius: 22px;
      display: flex; align-items: center; justify-content: center;
      margin: 0 auto; color: var(--bg-deep);
      box-shadow: 0 10px 25px -5px rgba(245, 158, 11, 0.4);
    }
    .digest-icon-wrap mat-icon { font-size: 36px; width: 36px; height: 36px; }

    .digest-form { text-align: left; }
    .privacy-note { font-size: 0.85rem; color: var(--text-muted); }
    .error-msg { display: block; margin-top: 8px; color: #ef4444; font-size: 0.85rem; }

    .digest-features { display: flex; flex-direction: column; gap: 16px; }
    .feature-card {
      display: flex; align-items: flex-start; gap: 20px;
      padding: 24px; border-radius: var(--radius-md); border-color: var(--glass-border-subtle);
    }
    .feature-icon {
      width: 48px; height: 48px; flex-shrink: 0;
      border-radius: 14px; background: rgba(245, 158, 11, 0.1);
      display: flex; align-items: center; justify-content: center; color: var(--primary);
    }
    .feature-card h4 { margin: 0 0 4px; font-size: 1.05rem; font-weight: 700; color: var(--text-primary); font-family: 'Inter', sans-serif; }
    .feature-card p { margin: 0; font-size: 0.9rem; line-height: 1.5; }

    @media (max-width: 800px) {
      .digest-layout { grid-template-columns: 1fr; }
    }
  `]
})
export class DigestComponent {
  loading = false;
  digestForm: FormGroup;

  features = [
    { icon: 'edit_note', title: 'Curated Stories', description: 'Handpicked by editors every Tuesday from the best community posts.' },
    { icon: 'trending_up', title: 'Trending Topics', description: 'Stay ahead with weekly summaries of what\'s gaining momentum.' },
    { icon: 'group', title: 'Author Spotlights', description: 'Discover new voices and writers pushing creative boundaries.' },
    { icon: 'lock', title: 'No Spam Guarantee', description: 'One email per week, zero spam, and an instant unsubscribe link.' }
  ];

  constructor(
    private fb: FormBuilder,
    private newsletterService: MediaNewsletterService,
    private toastr: ToastrService
  ) {
    this.digestForm = this.fb.group({
      fullName: [''],
      email: ['', [Validators.required, Validators.email]]
    });
  }

  onSubscribe(): void {
    if (this.digestForm.invalid) {
      this.digestForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const { fullName, email } = this.digestForm.value;

    this.newsletterService.subscribe({ email, fullName }).subscribe({
      next: (res: BaseResponse<string>) => {
        this.loading = false;
        if (res.success) {
          this.toastr.success('Subscription request sent! Wait for admin approval, then you will be subscribed.', 'Request Sent ✉️', { timeOut: 6000 });
          this.digestForm.reset();
        } else {
          this.toastr.error('Subscription failed. Please try again.');
        }
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Connection error. Please try again later.');
      }
    });
  }
}
