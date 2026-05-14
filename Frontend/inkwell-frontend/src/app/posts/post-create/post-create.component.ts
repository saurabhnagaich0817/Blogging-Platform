import { AfterViewInit, Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ToastrService } from 'ngx-toastr';
import { BaseResponse, MediaFile, Post } from '../../core/models/models';
import { MediaNewsletterService } from '../../core/services/media-newsletter.service';
import { PostService } from '../../core/services/post.service';
import { AuthService } from '../../core/services/auth.service';
import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models/models';

declare var Quill: any;

/**
 * Component responsible for creating new blog posts (stories).
 * Features rich text editing via Quill, image uploads to Media Service, 
 * and real-time validation.
 */
@Component({
  selector: 'app-post-create',
  template: `
    <div class="app-container py-60">
      <div class="flex-between mb-40 animate-in">
        <div>
          <div class="badge-amber mb-16">New Story</div>
          <h1 class="heading-xl">Write something <span class="text-gradient">worth reading.</span></h1>
        </div>
        <div class="flex items-center gap-16">
          <button type="button" class="btn-premium sm" [disabled]="loading" (click)="onSubmit()">
            <mat-spinner *ngIf="loading" diameter="16"></mat-spinner>
            <span *ngIf="!loading">Publish Now</span>
          </button>
          <button class="icon-btn-premium" routerLink="/dashboard" title="Back to Dashboard">
            <mat-icon>close</mat-icon>
          </button>
        </div>
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
              <h3>Add a cover image</h3>
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
            <span class="error-msg" *ngIf="postForm.get('title')?.touched && postForm.get('title')?.hasError('maxlength')">
              Keep the title under 160 characters.
            </span>
          </div>

          <div class="input-group mt-24">
            <label class="premium-label">Or paste an image URL</label>
            <div class="premium-input-box">
              <mat-icon>link</mat-icon>
              <input type="text" formControlName="imageUrl" placeholder="https://example.com/image.jpg" (input)="onImageUrlChange($event)">
            </div>
          </div>

          <div class="input-group mt-24">
            <div id="editor-container" class="editor-shell"></div>
            <span class="error-msg" *ngIf="contentError">{{ contentError }}</span>
          </div>

          <div class="input-group mt-24">
            <label class="premium-label">Select Category</label>
            <div class="premium-input-box">
              <mat-icon>category</mat-icon>
              <select formControlName="categoryId" class="premium-select">
                <option value="">No Category</option>
                <option *ngFor="let cat of categories" [value]="cat.categoryId">{{ cat.name }}</option>
              </select>
            </div>
          </div>

          <div class="form-footer mt-40">
            <button type="button" class="btn-premium outline" routerLink="/posts">Cancel</button>
            <button type="submit" class="btn-premium" [disabled]="loading">
              <mat-spinner *ngIf="loading" diameter="20"></mat-spinner>
              <span *ngIf="!loading">Publish Story</span>
              <mat-icon *ngIf="!loading">publish</mat-icon>
            </button>
          </div>
        </form>

        <aside class="create-sidebar animate-in" style="animation-delay: 0.1s">
          <div class="glass-card p-32 sticky-sidebar">
            <h4 class="heading-sm mb-20 flex items-center gap-8 text-amber"><mat-icon>lightbulb</mat-icon> Writing Tips</h4>
            <ul class="tips-list">
              <li>Use a clear, compelling title.</li>
              <li>Break up long paragraphs to improve readability.</li>
              <li>Use headings (H1, H2) to structure your thoughts.</li>
              <li>Add a high-quality cover image to stand out in the feed.</li>
            </ul>
            
            <div class="divider-amber my-24"></div>
            
            <div class="info-block">
              <mat-icon>info</mat-icon>
              <p>Your story will be published immediately to the community feed.</p>
            </div>
          </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .create-grid { display: grid; grid-template-columns: minmax(0, 1fr) 350px; gap: 40px; align-items: start; }

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
    .story-title-input::placeholder { color: rgba(255,255,255,0.15); }

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
    ::ng-deep .ql-editor.ql-blank::before { color: rgba(255,255,255,0.2); font-style: normal; }
    ::ng-deep .ql-snow .ql-stroke { stroke: var(--text-secondary); }
    ::ng-deep .ql-snow .ql-fill { fill: var(--text-secondary); }
    ::ng-deep .ql-snow .ql-picker { color: var(--text-secondary); }
    ::ng-deep .ql-toolbar.ql-snow button:hover .ql-stroke { stroke: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow button:hover .ql-fill { fill: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow .ql-picker-label:hover { color: var(--primary); }
    ::ng-deep .ql-toolbar.ql-snow .ql-picker-label:hover .ql-stroke { stroke: var(--primary); }

    .form-footer { display: flex; justify-content: flex-end; gap: 16px; align-items: center; }
    
    .sticky-sidebar { position: sticky; top: 100px; }
    .tips-list { list-style: none; padding: 0; margin: 0; display: grid; gap: 16px; }
    .tips-list li { position: relative; padding-left: 28px; color: var(--text-secondary); font-size: 0.95rem; line-height: 1.5; }
    .tips-list li::before { content: 'check'; font-family: 'Material Icons Round'; position: absolute; left: 0; top: -2px; color: #22c55e; font-size: 20px; }

    .info-block { display: flex; gap: 12px; align-items: flex-start; padding: 16px; background: rgba(59,130,246,0.1); border-radius: var(--radius-md); color: #93c5fd; }
    .info-block mat-icon { color: #60a5fa; }
    .info-block p { margin: 0; font-size: 0.85rem; line-height: 1.5; }

    .premium-label { display: block; font-size: 0.85rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; margin-bottom: 8px; letter-spacing: 0.05em; }
    .premium-input-box { display: flex; align-items: center; gap: 12px; background: rgba(255,255,255,0.03); border: 1px solid var(--glass-border-subtle); border-radius: var(--radius-md); padding: 0 16px; transition: all 0.3s; }
    .premium-input-box:focus-within { border-color: var(--primary); background: rgba(245,158,11,0.05); }
    .premium-input-box mat-icon { color: var(--text-muted); font-size: 20px; width: 20px; height: 20px; }
    .premium-input-box input { flex: 1; background: transparent; border: none; outline: none; padding: 12px 0; color: var(--text-primary); font-size: 0.95rem; }
    .premium-select { flex: 1; background: transparent; border: none; outline: none; padding: 12px 0; color: var(--text-primary); font-size: 0.95rem; appearance: none; cursor: pointer; }
    .premium-select option { background: #1a1a2e; color: white; }

    @media (max-width: 1000px) {
      .create-grid { grid-template-columns: 1fr; }
      .create-sidebar { display: none; }
      .story-title-input { font-size: 2rem; }
      .premium-upload-zone { height: 240px; }
    }
  `]
})
export class PostCreateComponent implements AfterViewInit {
  postForm: FormGroup;
  loading = false;
  contentError = '';
  selectedFile: File | null = null;
  imagePreview: string | null = null;
  quill: any;
  categories: Category[] = [];

  constructor(
    private fb: FormBuilder,
    private postService: PostService,
    private mediaService: MediaNewsletterService,
    private authService: AuthService,
    private categoryService: CategoryService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.postForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(160)]],
      content: ['', Validators.required],
      imageUrl: [''],
      categoryId: ['']
    });
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.categories = res.data;
        }
      }
    });
  }

  ngAfterViewInit(): void {
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

    this.quill.on('text-change', () => {
      this.syncEditorContent();
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) return;

    this.selectedFile = file;
    const reader = new FileReader();
    reader.onload = () => {
      this.imagePreview = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  onImageUrlChange(event: any): void {
    const url = event.target.value;
    if (url && url.startsWith('http')) {
      this.imagePreview = url;
      this.selectedFile = null;
    }
  }

  removeImage(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.postForm.patchValue({ imageUrl: '' });
  }

  /**
   * Handles form submission. Validates content, uploads images if necessary, 
   * and then creates the post.
   */
  onSubmit(): void {
    this.postForm.markAllAsTouched();
    this.syncEditorContent();

    if (!this.validateEditorContent() || this.postForm.invalid) return;

    this.loading = true;

    if (!this.selectedFile) {
      this.createPost(this.postForm.value.imageUrl || '');
      return;
    }

    this.mediaService.uploadMedia(this.selectedFile).subscribe({
      next: (response: BaseResponse<MediaFile>) => {
        if (response.success && response.data?.url) {
          this.createPost(response.data.url);
          return;
        }

        this.loading = false;
        this.toastr.error(response.message || 'Image upload failed.', 'Upload Failed');
      },
      error: (error) => {
        this.loading = false;
        this.toastr.error('Could not upload the selected image.', 'Upload Failed');
      }
    });
  }

  /**
   * Sends the final post data to the Post Service.
   * @param imageUrl The URL of the successfully uploaded or provided image.
   */
  private createPost(imageUrl: string): void {
    const user = this.authService.getCurrentUser();
    const selectedCategory = this.categories.find(c => c.categoryId === this.postForm.value.categoryId);
    
    const request = {
      title: this.postForm.value.title.trim(),
      content: this.postForm.value.content,
      imageUrl,
      status: 'Published',
      authorName: user?.fullName || user?.username || 'Anonymous',
      categoryId: selectedCategory?.categoryId,
      categoryName: selectedCategory?.name
    };

    this.postService.createPost(request)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response: BaseResponse<Post>) => {
          if (response.success && response.data) {
            this.toastr.success('Story published successfully.');
            this.router.navigate(['/posts', response.data.postId]);
          } else {
            this.toastr.error('Publish failed.');
          }
        },
        error: (error) => {
          this.toastr.error(error.error?.message || 'The story could not be published.');
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
