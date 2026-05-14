import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, Post, CreatePostRequest, UpdatePostRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class PostService {
  private apiUrl = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  getAllPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(this.apiUrl);
  }

  getPostById(id: string): Observable<BaseResponse<Post>> {
    return this.http.get<BaseResponse<Post>>(`${this.apiUrl}/${id}`);
  }

  createPost(request: CreatePostRequest): Observable<BaseResponse<Post>> {
    return this.http.post<BaseResponse<Post>>(this.apiUrl, request);
  }

  updatePost(id: string, request: UpdatePostRequest): Observable<BaseResponse<Post>> {
    return this.http.put<BaseResponse<Post>>(`${this.apiUrl}/${id}`, request);
  }

  deletePost(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/${id}`);
  }

  toggleLike(id: string): Observable<BaseResponse<number>> {
    return this.http.post<BaseResponse<number>>(`${this.apiUrl}/${id}/like`, {});
  }

  toggleSave(id: string): Observable<BaseResponse<boolean>> {
    return this.http.post<BaseResponse<boolean>>(`${this.apiUrl}/${id}/save`, {});
  }

  sharePost(id: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${this.apiUrl}/${id}/share`, {});
  }

  getMyPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(`${this.apiUrl}/my`);
  }

  getSavedPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(`${this.apiUrl}/saved`);
  }

  getAnalytics(): Observable<BaseResponse<any>> {
    return this.http.get<BaseResponse<any>>(`${this.apiUrl}/analytics`);
  }
}
