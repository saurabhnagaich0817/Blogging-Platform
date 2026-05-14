import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let routerSpy: jasmine.SpyObj<Router>;

  beforeEach(() => {
    const spy = jasmine.createSpyObj('Router', ['navigate']);
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: Router, useValue: spy }
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    
    // Clear localStorage before each test
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Login', () => {
    it('should login and set token in localStorage on success', () => {
    const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.sig'; 
      const mockResponse = {
        success: true,
        message: 'Logged in',
        data: { token: mockToken }
      };

      service.login('test@example.com', 'password').subscribe(response => {
        expect(response.success).toBeTrue();
        expect(response.data!.token).toBe(mockToken);
        expect(localStorage.getItem('inkwell_token')).toBe(mockToken);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
    });

    it('should not set token if login fails', () => {
      const mockResponse = {
        success: false,
        message: 'Invalid credentials'
      };

      service.login('test@example.com', 'wrong').subscribe(response => {
        expect(response.success).toBeFalse();
        expect(localStorage.getItem('inkwell_token')).toBeNull();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
      req.flush(mockResponse);
    });
  });

  describe('Token Management', () => {
    it('should return token from localStorage', () => {
      localStorage.setItem('inkwell_token', 'stored-token');
      expect(service.getToken()).toBe('stored-token');
    });

    it('should clear token and navigate to login on logout', () => {
      localStorage.setItem('inkwell_token', 'some-token');
      service.logout();
      expect(localStorage.getItem('inkwell_token')).toBeNull();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/auth/login']);
    });
  });

  describe('Registration', () => {
    it('should send registration request to correct endpoint', () => {
      const mockResponse = { success: true };
      
      service.register('user1', 'user@test.com', 'pass123').subscribe(res => {
        expect(res.success).toBeTrue();
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/auth/register`);
      expect(req.request.body).toEqual(jasmine.objectContaining({
        username: 'user1',
        email: 'user@test.com'
      }));
      req.flush(mockResponse);
    });
  });
});
