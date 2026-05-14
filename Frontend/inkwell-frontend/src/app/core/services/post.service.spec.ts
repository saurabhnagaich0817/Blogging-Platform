import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PostService } from './post.service';
import { environment } from '../../../environments/environment';

describe('PostService', () => {
  let service: PostService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [PostService]
    });
    service = TestBed.inject(PostService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch all posts', () => {
    const mockPosts = {
      success: true,
      data: [
        { id: '1', title: 'Test Post 1', content: 'Content 1' },
        { id: '2', title: 'Test Post 2', content: 'Content 2' }
      ]
    };

    service.getAllPosts().subscribe(res => {
      expect(res.success).toBeTrue();
      expect(res.data!.length).toBe(2);
      expect(res.data![0].title).toBe('Test Post 1');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/posts`);
    expect(req.request.method).toBe('GET');
    req.flush(mockPosts);
  });

  it('should handle error when fetching posts fails', () => {
    service.getAllPosts().subscribe(
      () => fail('should have failed with 500 error'),
      (error) => {
        expect(error.status).toBe(500);
      }
    );

    const req = httpMock.expectOne(`${environment.apiUrl}/posts`);
    req.flush('Error', { status: 500, statusText: 'Internal Server Error' });
  });

  it('should create a new post', () => {
    const newPostRequest = { title: 'New', content: 'New Content', categoryId: 'cat1' };
    const mockResponse = { success: true, data: { postId: '3', ...newPostRequest } };

    service.createPost(newPostRequest as any).subscribe(res => {
      expect(res.success).toBeTrue();
      expect(res.data!.postId).toBe('3');
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/posts`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newPostRequest);
    req.flush(mockResponse);
  });

  it('should toggle like status', () => {
    const mockResponse = { success: true, data: 10 }; // 10 likes now
    
    service.toggleLike('post123').subscribe(res => {
      expect(res.success).toBeTrue();
      expect(res.data).toBe(10);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/posts/post123/like`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });
});
