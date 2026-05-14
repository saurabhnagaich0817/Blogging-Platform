import { AfterViewInit, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { BaseResponse, MediaFile, Post } from '../../core/models/models';
import { MediaNewsletterService } from '../../core/services/media-newsletter.service';
import { PostService } from '../../core/services/post.service';
import { AuthService } from '../../core/services/auth.service';
import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models/models';

declare var Quill: any;

@Component({
  selector: 'app-post-edit',
  template: `
    <div *ngIf="initialLoading" class="story-loading">
      <div class="loading-art"></div>
      <h3>Loading story editor...</h3>
    </div>

    <div *ngIf="!initialLoading && !post" class="app-container py-60">
      <div class="glass-card state-card animate-in">
        <mat-icon>error_outline</mat-icon>
        <h2>Story not found</h2>
        <p>This story might have been deleted or you don't have permission to edit it.</p>
        <button class="btn-premium outline mt-24" routerLink="/posts">Back to Stories</button>
      </div>
    </div>

    <div *ngIf="!initialLoading && post" class="app-container py-60">
      <div class="flex-between mb-40 animate-in">
        <div>
          <div class="badge-amber mb-16">Editing Story</div>
          <h1 class="heading-xl">Refine your <span class="text-gradient">masterpiece.</span></h1>
        </div>
        <button class="icon-btn-premium" [routerLink]="['/posts', post.postId]" title="Cancel Editing">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="create-grid">
        <form [formGroup]="postForm" (ngSubmit)="onSubmit()" class="glass-card p-40 create-main-form animate-in">
          
          <div class="premium-upload-zone mb-40" [class.has-image]="imagePreview" (click)="!imagePreview && fileInput.click()">
            <img *ngIf="imagePreview" [src]="imagePreview" class="upload-preview" alt="Selected cover image">
            <div class="upload-overlay" *ngIf="imagePreview">
              <button type="button" class="btn-premium danger sm" (click)="$event.stopPropagation(); removeImage()">
                <mat-icon>delete</mat-icon> Remove Cover
              </button>
            </div>
            <div class="upload-prompt" *ngIf="!imagePreview">
              <div class="prompt-icon">
                <mat-icon>add_photo_alternate</mat-icon>
              </div>
              <h3>Update cover image</h3>
              <p>Optional, but highly recommended for engagement.</p>
            </div>
            <input #fileInput type="file" (change)="onFileSelected($event)" accept="image/*" hidden>
          </div>

          <div class="input-group">
            <input
              type="text"
              class="story-title-input"
              formControlName="title"
              placeholder="Story Title...">
            <span class="error-msg" *ngIf="postForm.get('title')?.touched && postForm.get('title')?.hasError('required')">
              Title is required.
            </span>
          </div>

          <div class="input-group mt-24">
            <div id="editor-container" class="editor-shell"></div>
            <span class="error-msg" *ngIf="contentError">{{ contentError }}</span>
          </div>

          <div class="input-group mt-24">
            <label class="premium-label" style="display: block; font-size: 0.85rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; margin-bottom: 8px;">Select Category</label>
            <div class="premium-input-box" style="display: flex; align-items: center; gap: 12px; background: rgba(255,255,255,0.03); border: 1px solid var(--glass-border-subtle); border-radius: var(--radius-md); padding: 0 16px;">
              <mat-icon style="color: var(--text-muted);">category</mat-icon>
              <select formControlName="categoryId" class="premium-select" style="flex: 1; background: transparent; border: none; outline: none; padding: 12px 0; color: var(--text-primary); font-size: 0.95rem; appearance: none; cursor: pointer;">
                <option value="">No Category</option>
                <option *ngFor="let cat of categories" [value]="cat.categoryId">{{ cat.name }}</option>
              </select>
            </div>
          </div>

          <div class="form-footer mt-40">
            <button type="button" class="btn-premium danger outline mr-auto" (click)="deletePost()">
              <mat-icon>delete</mat-icon> Delete Story
            </button>
            <button type="button" class="btn-premium outline" [routerLink]="['/posts', post.postId]">Cancel</button>
            <button type="submit" class="btn-premium" [disabled]="loading || postForm.pristine && !imageChanged">
              <mat-spinner *ngIf="loading" diameter="20"></mat-spinner>
              <span *ngIf="!loading">Save Changes</span>
              <mat-icon *ngIf="!loading">save</mat-icon>
            </button>
          </div>
        </form>

        <aside class="create-sidebar animate-in" style="animation-delay: 0.1s">
          <div class="glass-card p-32 sticky-sidebar">
            <h4 class="heading-sm mb-20 flex items-center gap-8 text-amber"><mat-icon>update</mat-icon> Update Info</h4>
            
            <div class="info-block mb-24" style="background: rgba(34,197,94,0.1); color: #86efac;">
              <mat-icon style="color: #22c55e;">check_circle</mat-icon>
              <p>Changes you make will be live immediately after saving.</p>
            </div>

            <div class="id-item mb-12">
              <span class="id-label" style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase;">Published On</span>
              <span class="id-value" style="font-weight: 600; color: var(--text-primary)">{{ post.createdAt | date:'mediumDate' }}</span>
            </div>
            
            <div class="id-item" *ngIf="post.updatedAt">
              <span class="id-label" style="font-size: 0.75rem; color: var(--text-muted); text-transform: uppercase;">Last Edited</span>
              <span class="id-value" style="font-weight: 600; color: var(--text-primary)">{{ post.updatedAt | date:'mediumDate' }}</span>
            </div>
          </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .create-grid { display: grid; grid-template-columns: minmax(0, 1fr) 350px; gap: 40px; align-items: start; }
    .mr-auto { margin-right: auto; }

    .premium-upload-zone {
      width: 100%; height: 360px; border: 2px dashed var(--glass-border-subtle); border-radius: var(--radius-lg);
      background: rgba(255,255,255,0.02); display: flex; align-items: center; justify-content: center;
      position: relative; overflow: hidden; cursor: pointer; transition: all 0.3s ease;
    }
    .premium-upload-zone:hover { border-color: var(--primary); background: rgba(245,158,11,0.04); }
    .premium-upload-zone.has-image { border: none; }
    
    .upload-preview { width: 100%; height: 100%; object-fit: cover; }
    .upload-overlay {
      position: absolute; inset: 0; display: flex; align-items: center; justify-content: center;
      background: rgba(8,8,16,0.6); opacity: 0; transition: opacity 0.2s ease; backdrop-filter: blur(4px);
    }
    .premium-upload-zone:hover .upload-overlay { opacity: 1; }
    
    .upload-prompt { text-align: center; }
    .prompt-icon {
      width: 64px; height: 64px; margin: 0 auto 16px; border-radius: 20px; background: rgba(255,255,255,0.05);
      display: flex; align-items: center; justify-content: center; color: var(--text-secondary); transition: all 0.3s;
    }
    .premium-upload-zone:hover .prompt-icon { color: var(--primary); transform: scale(1.1); }
    .upload-prompt h3 { margin: 0 0 8px; font-family: 'Inter', sans-serif; font-size: 1.1rem; }
    .upload-prompt p { margin: 0; color: var(--text-muted); font-size: 0.9rem; }

    .story-title-input {
      width: 100%; background: transparent; border: none; outline: none; border-bottom: 2px solid var(--glass-border-subtle);
      padding: 16px 0; font-family: 'Playfair Display', serif; font-size: 2.8rem; font-weight: 800; color: var(--text-primary);
      transition: border-color 0.3s;
    }
    .story-title-input:focus { border-color: var(--primary); }

    .editor-shell {
      min-height: 500px; border: none; border-bottom: 1px solid var(--glass-border-subtle);
      font-family: 'Inter', sans-serif; font-size: 1.1rem; line-height: 1.8; color: var(--text-primary);
    }
    ::ng-deep .ql-toolbar.ql-snow {
      border: none !important; border-bottom: 1px solid var(--glass-border-subtle) !important;
      padding: 12px 0 !important; background: transparent !important; margin-bottom: 20px;
    }
    ::ng-deep .ql-container.ql-snow { border: none !important; font-family: 'Inter', sans-serif; font-size: 1.1rem; }
    ::ng-deep .ql-editor { padding: 0 !important; min-height: 400px; }
    ::ng-deep .ql-snow .ql-stroke { stroke: var(--text-secondary); }
    ::ng-deep .ql-snow .ql-fill { fill: var(--text-secondary); }
    ::ng-deep .ql-snow .ql-picker { color: var(--text-secondary); }
    ::ng-deep .ql-toolbar.ql-snow button:hover .ql-stroke { stroke: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow button:hover .ql-fill { fill: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow .ql-picker-label:hover { color: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow .ql-picker-label:hover .ql-stroke { stroke: var(--primary); }

    .form-footer { display: flex; justify-content: flex-end; gap: 16px; align-items: center; }
    
    .sticky-sidebar { position: sticky; top: 100px; }
    .info-block { display: flex; gap: 12px; align-items: flex-start; padding: 16px; border-radius: var(--radius-md); }
    .info-block p { margin: 0; font-size: 0.85rem; line-height: 1.5; }

    .id-item { display: flex; justify-content: space-between; align-items: center; }

    @media (max-width: 1000px) {
      .create-grid { grid-template-columns: 1fr; }
      .create-sidebar { display: none; }
      .story-title-input { font-size: 2rem; }
      .premium-upload-zone { height: 240px; }
      .form-footer { flex-wrap: wrap; }
      .mr-auto { margin-right: 0; width: 100%; margin-bottom: 16px; }
    }
  `]
})
export class PostEditComponent implements OnInit, AfterViewInit {
  postForm: FormGroup;
  loading = false;
  initialLoading = true;
  contentError = '';
  
  post: Post | null = null;
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  imageChanged = false;
  quill: any;
  categories: Category[] = [];

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private postService: PostService,
    private authService: AuthService,
    private categoryService: CategoryService,
    private mediaService: MediaNewsletterService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.postForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(160)]],
      content: ['', Validators.required],
      categoryId: ['']
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    this.loadCategories();
    if (!id) {
      this.initialLoading = false;
      return;
    }

    this.postService.getPostById(id).subscribe({
      next: (res) => {
        this.post = res.data;
        
        this.postForm.patchValue({
          title: this.post?.title,
          content: this.post?.content,
          categoryId: this.post?.categoryId || ''
        });
        this.imagePreview = this.post?.imageUrl || null;
        
        // Safety check for authorship (case-insensitive)
        const user = this.authService.getCurrentUser();
        if (user && this.post && user.id.toLowerCase() !== this.post.authorId.toLowerCase()) {
          this.toastr.error('You do not have permission to edit this post.');
          this.router.navigate(['/posts']);
          this.initialLoading = false;
          return;
        }

        // If quill is already loaded, set content
        if (this.quill && this.post?.content) {
          this.quill.clipboard.dangerouslyPasteHTML(this.post.content);
        }

        this.initialLoading = false;
        
        // Use timeout to ensure DOM is updated before initializing Quill
        setTimeout(() => this.initializeQuill(), 100);
      },
      error: () => {
        this.initialLoading = false;
        this.toastr.error('Failed to load post for editing.');
      }
    });
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (res) => { this.categories = res.data || []; }
    });
  }

  ngAfterViewInit(): void {
    // Moved to initializeQuill after data load
  }

  private initializeQuill(): void {
    const container = document.getElementById('editor-container');
    if (!container) return;

    if (typeof Quill === 'undefined') {
      this.contentError = 'The rich text editor failed to load.';
      return;
    }

    this.quill = new Quill('#editor-container', {
      theme: 'snow',
      placeholder: 'Tell your story...',
      modules: {
        toolbar: [
          [{ header: [1, 2, 3, false] }],
          ['bold', 'italic', 'underline', 'strike'],
          ['blockquote', 'code-block'],
          [{ list: 'ordered' }, { list: 'bullet' }],
          ['link', 'clean']
        ]
      }
    });

    if (this.post?.content) {
      this.quill.clipboard.dangerouslyPasteHTML(this.post.content);
    }

    this.quill.on('text-change', () => {
      this.syncEditorContent();
      this.postForm.markAsDirty();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    this.selectedFile = file;
    this.imageChanged = true;
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreview = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  removeImage(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.imageChanged = true;
  }

  onSubmit(): void {
    if (!this.post) return;
    
    this.postForm.markAllAsTouched();
    this.syncEditorContent();

    if (!this.validateEditorContent() || this.postForm.invalid) return;

    this.loading = true;

    // Handle image upload if changed
    if (this.imageChanged && this.selectedFile) {
      this.mediaService.uploadMedia(this.selectedFile).subscribe({
        next: (response: BaseResponse<MediaFile>) => {
          if (response.success && response.data?.url) {
            this.updatePost(response.data.url);
          } else {
            this.loading = false;
            this.toastr.error('Image upload failed.');
          }
        },
        error: () => {
          this.loading = false;
          this.toastr.error('Could not upload the selected image.');
        }
      });
    } else {
      // Keep existing image or null
      this.updatePost(this.imageChanged ? '' : (this.post.imageUrl || ''));
    }
  }

  private updatePost(imageUrl: string): void {
    if (!this.post) return;

    const selectedCategory = this.categories.find(c => c.categoryId === this.postForm.value.categoryId);

    const request = {
      title: this.postForm.value.title.trim(),
      content: this.postForm.value.content,
      imageUrl: imageUrl,
      categoryId: selectedCategory?.categoryId,
      categoryName: selectedCategory?.name
    };

    this.postService.updatePost(this.post.postId, request)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response: BaseResponse<Post>) => {
          if (response.success) {
            this.toastr.success('Story updated successfully.');
            this.router.navigate(['/posts', this.post?.postId]);
          } else {
            this.toastr.error('Update failed.');
          }
        },
        error: () => {
          this.toastr.error('The story could not be updated.');
        }
      });
  }

  deletePost(): void {
    if (!this.post || !confirm('Delete this story permanently? This cannot be undone.')) return;

    this.loading = true;
    this.postService.deletePost(this.post.postId).subscribe({
      next: () => {
        this.toastr.success('Story deleted successfully.');
        this.router.navigate(['/posts/my']);
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Could not delete story.');
      }
    });
  }

  private syncEditorContent(): void {
    if (!this.quill) return;
    const html = this.quill.root.innerHTML?.trim() || '';
    this.postForm.patchValue({ content: html }, { emitEvent: false });
  }

  private validateEditorContent(): boolean {
    const plainText = this.quill?.getText()?.trim() || '';
    if (!plainText) {
      this.contentError = 'Story content cannot be empty.';
      return false;
    }
    this.contentError = '';
    return true;
  }
}
