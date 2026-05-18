import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, Post, CreatePostRequest, UpdatePostRequest } from '../models/models';

/**
 * Service responsible for interacting with the Post microservice via the API Gateway.
 */
@Injectable({ providedIn: 'root' })
export class PostService {
  private apiUrl = `${environment.apiUrl}/posts`;

  constructor(private http: HttpClient) {}

  /**
   * Fetches all published blog posts.
   */
  getAllPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(this.apiUrl);
  }

  /**
   * Fetches a specific blog post by its unique ID.
   */
  getPostById(id: string): Observable<BaseResponse<Post>> {
    return this.http.get<BaseResponse<Post>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Creates a new blog post.
   */
  createPost(request: CreatePostRequest): Observable<BaseResponse<Post>> {
    return this.http.post<BaseResponse<Post>>(this.apiUrl, request);
  }

  /**
   * Updates an existing blog post.
   */
  updatePost(id: string, request: UpdatePostRequest): Observable<BaseResponse<Post>> {
    return this.http.put<BaseResponse<Post>>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Deletes a blog post permanently.
   */
  deletePost(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/${id}`);
  }

  /**
   * Toggles the like status of a post for the current user.
   */
  toggleLike(id: string): Observable<BaseResponse<number>> {
    return this.http.post<BaseResponse<number>>(`${this.apiUrl}/${id}/like`, {});
  }

  /**
   * Toggles the saved status of a post.
   */
  toggleSave(id: string): Observable<BaseResponse<boolean>> {
    return this.http.post<BaseResponse<boolean>>(`${this.apiUrl}/${id}/save`, {});
  }

  /**
   * Records a share action for the specified post.
   */
  sharePost(id: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${this.apiUrl}/${id}/share`, {});
  }

  /**
   * Retrieves all posts authored by the current user.
   */
  getMyPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(`${this.apiUrl}/my`);
  }

  /**
   * Retrieves all posts saved by the current user.
   */
  getSavedPosts(): Observable<BaseResponse<Post[]>> {
    return this.http.get<BaseResponse<Post[]>>(`${this.apiUrl}/saved`);
  }

  /**
   * Fetches analytics data for the dashboard.
   */
  getAnalytics(): Observable<BaseResponse<any>> {
    return this.http.get<BaseResponse<any>>(`${this.apiUrl}/analytics`);
  }
}
