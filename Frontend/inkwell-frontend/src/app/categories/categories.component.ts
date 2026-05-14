/* 
  UI TEMPLATE STRUCTURE:
  1. Full screen container
  2. Material Card wrapper
  3. Clean header with icon + title
  4. Form fields with spacing
  5. Primary gradient button
  6. Loading spinner
  7. Error messages
  8. Success toast notification
  9. Responsive layout
*/

import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CategoryService } from '../core/services/category.service';
import { AuthService } from '../core/services/auth.service';
import { Category, Tag } from '../core/models/models';

@Component({
  selector: 'app-categories',
  template: `
    <div class="app-container py-60">
      <header class="page-header mb-40 animate-in">
        <div class="badge-premium">Organization</div>
        <h1 class="heading-xl">Content <span class="text-gradient">Taxonomy</span></h1>
        <p class="text-secondary subtitle-xl">Define the structure and discoverability of your platform.</p>
      </header>

      <div class="taxonomy-grid">
        <!-- CATEGORIES -->
        <div class="taxonomy-section animate-in">
          <div class="glass-card p-32 h-full">
            <div class="section-top mb-24">
               <h2 class="heading-sm"><mat-icon>folder_special</mat-icon> Categories</h2>
               <p class="text-muted">High-level buckets for content organization.</p>
            </div>

            <form [formGroup]="catForm" (ngSubmit)="createCategory()" *ngIf="isAdmin" class="premium-inline-form mb-32">
              <div class="premium-input-box">
                <input type="text" formControlName="name" placeholder="New Category Name">
                <button type="submit" [disabled]="catForm.invalid || saving">
                   <mat-spinner *ngIf="saving" diameter="18"></mat-spinner>
                   <mat-icon *ngIf="!saving">add</mat-icon>
                </button>
              </div>
            </form>

            <div class="data-list-premium">
              <div *ngIf="catLoading" class="p-40 text-center"><mat-spinner diameter="30" class="mx-auto"></mat-spinner></div>
              
              <div *ngFor="let cat of categories" class="data-node">
                <div class="node-main">
                  <span class="name">{{ cat.name }}</span>
                  <span class="slug">/{{ cat.slug }}</span>
                </div>
                <button class="node-action-btn danger" *ngIf="isAdmin" (click)="deleteCategory(cat)">
                   <mat-icon>delete_outline</mat-icon>
                </button>
              </div>

              <div *ngIf="!catLoading && categories.length === 0" class="empty-state-mini">
                 <p>No categories defined.</p>
              </div>
            </div>
          </div>
        </div>

        <!-- TAGS -->
        <div class="taxonomy-section animate-in" style="animation-delay: 0.1s">
          <div class="glass-card p-32 h-full">
            <div class="section-top mb-24">
               <h2 class="heading-sm"><mat-icon>tag</mat-icon> Popular Tags</h2>
               <p class="text-muted">Granular labels for cross-cutting interests.</p>
            </div>

            <form [formGroup]="tagForm" (ngSubmit)="createTag()" *ngIf="isAdmin" class="premium-inline-form mb-32">
               <div class="premium-input-box">
                 <input type="text" formControlName="name" placeholder="Add Tag (e.g. Angular)">
                 <button type="submit" [disabled]="tagForm.invalid">
                    <mat-icon>tag</mat-icon>
                 </button>
               </div>
            </form>

            <div class="tag-cloud-premium">
              <div *ngIf="tagLoading" class="p-40 text-center"><mat-spinner diameter="30" class="mx-auto"></mat-spinner></div>
              
              <div *ngFor="let tag of tags" class="cloud-tag">
                 <span class="hash">#</span>
                 <span class="label">{{ tag.name }}</span>
                 <span class="count" *ngIf="tag.postCount > 0">{{ tag.postCount }}</span>
              </div>

              <div *ngIf="!tagLoading && tags.length === 0" class="empty-state-mini">
                 <p>No tags found.</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .py-60 { padding-top: 60px; padding-bottom: 60px; }
    .mb-40 { margin-bottom: 40px; }
    .mb-32 { margin-bottom: 32px; }
    .mb-24 { margin-bottom: 24px; }
    .p-32 { padding: 32px; }
    .p-40 { padding: 40px; }
    .h-full { height: 100%; }
    .mx-auto { margin-left: auto; margin-right: auto; }

    .page-header { text-align: center; border-bottom: 1px solid var(--glass-border); padding-bottom: 40px; }
    .badge-premium { display: inline-block; padding: 4px 12px; background: rgba(99, 102, 241, 0.1); color: var(--primary); border-radius: 20px; font-size: 0.8rem; font-weight: 700; text-transform: uppercase; margin-bottom: 12px; }
    .subtitle-xl { font-size: 1.2rem; margin-top: 12px; }

    .taxonomy-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 32px; }

    .section-top h2 { display: flex; align-items: center; gap: 10px; margin-bottom: 4px; }
    .section-top mat-icon { color: var(--primary); }

    .premium-inline-form .premium-input-box { display: flex; align-items: center; }
    .premium-inline-form input { flex: 1; background: none; border: none; color: white; padding: 12px; outline: none; }
    .premium-inline-form button { background: var(--grad-main); border: none; color: white; width: 44px; height: 44px; border-radius: 10px; cursor: pointer; display: flex; align-items: center; justify-content: center; }
    .premium-inline-form button:disabled { opacity: 0.5; cursor: not-allowed; }

    .data-list-premium { display: flex; flex-direction: column; gap: 8px; }
    .data-node { display: flex; justify-content: space-between; align-items: center; padding: 14px 20px; background: rgba(255,255,255,0.02); border: 1px solid var(--glass-border); border-radius: 12px; transition: all 0.3s; }
    .data-node:hover { border-color: var(--primary); background: rgba(255,255,255,0.05); }
    .node-main { display: flex; flex-direction: column; }
    .node-main .name { font-weight: 700; font-size: 1rem; }
    .node-main .slug { font-size: 0.75rem; color: var(--text-muted); }
    .node-action-btn { background: none; border: none; color: var(--text-muted); cursor: pointer; transition: color 0.3s; }
    .node-action-btn.danger:hover { color: #ef4444; }

    .tag-cloud-premium { display: flex; flex-wrap: wrap; gap: 10px; }
    .cloud-tag { 
      padding: 8px 16px; background: rgba(255,255,255,0.03); border: 1px solid var(--glass-border);
      border-radius: 30px; display: flex; align-items: center; gap: 6px; font-weight: 600; font-size: 0.85rem;
      transition: all 0.3s; cursor: default;
    }
    .cloud-tag:hover { background: var(--grad-main); border-color: transparent; transform: translateY(-2px); }
    .cloud-tag .hash { color: var(--primary); opacity: 0.6; }
    .cloud-tag:hover .hash, .cloud-tag:hover .label { color: white; }
    .cloud-tag .count { font-size: 0.7rem; background: rgba(255,255,255,0.1); padding: 1px 6px; border-radius: 10px; }

    .empty-state-mini { text-align: center; padding: 20px; color: var(--text-muted); font-style: italic; }

    @media (max-width: 800px) {
      .taxonomy-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class CategoriesComponent implements OnInit {
  categories: Category[] = [];
  tags: Tag[] = [];
  catLoading = false;
  tagLoading = false;
  saving = false;
  catForm: FormGroup;
  tagForm: FormGroup;

  get isAdmin(): boolean { return this.authService.getUserRole() === 'Admin'; }

  constructor(
    private categoryService: CategoryService,
    private authService: AuthService,
    private fb: FormBuilder,
    private toastr: ToastrService
  ) {
    this.catForm = this.fb.group({ name: ['', Validators.required] });
    this.tagForm = this.fb.group({ name: ['', Validators.required] });
  }

  ngOnInit(): void { this.loadCategories(); this.loadTags(); }

  loadCategories(): void {
    this.catLoading = true;
    this.categoryService.getAllCategories().subscribe({
      next: (res) => { this.catLoading = false; this.categories = res.data || []; },
      error: () => { this.catLoading = false; }
    });
  }

  loadTags(): void {
    this.tagLoading = true;
    this.categoryService.getAllTags().subscribe({
      next: (res) => { this.tagLoading = false; this.tags = res.data || []; },
      error: () => { this.tagLoading = false; }
    });
  }

  createCategory(): void {
    if (this.catForm.invalid) return;
    this.saving = true;
    this.categoryService.createCategory({ name: this.catForm.value.name }).subscribe({
      next: (res) => {
        this.saving = false;
        if (res.success && res.data) {
          this.categories.unshift(res.data);
          this.catForm.reset();
          this.toastr.success('Category created successfully!', 'Success');
        }
      },
      error: () => { this.saving = false; this.toastr.error('Failed to create category', 'Error'); }
    });
  }

  deleteCategory(cat: Category): void {
    if (!confirm(`Permanently delete category "${cat.name}"?`)) return;
    this.categoryService.deleteCategory(cat.categoryId).subscribe({
      next: () => {
        this.categories = this.categories.filter(c => c.categoryId !== cat.categoryId);
        this.toastr.info('Category removed.', 'Deleted');
      },
      error: () => this.toastr.error('Could not delete category', 'Error')
    });
  }

  createTag(): void {
    if (this.tagForm.invalid) return;
    this.categoryService.createTag(this.tagForm.value.name).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.tags.unshift(res.data);
          this.tagForm.reset();
          this.toastr.success('New tag added!', 'Success');
        }
      },
      error: () => this.toastr.error('Failed to create tag', 'Error')
    });
  }
}
