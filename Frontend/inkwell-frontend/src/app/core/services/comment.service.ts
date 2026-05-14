import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BaseResponse, Comment, CreateCommentRequest
} from '../models/models';

@Injectable({ providedIn: 'root' })
export class CommentService {
  private apiUrl = `${environment.apiUrl}/comments`;

  constructor(private http: HttpClient) {}

  /** GET /api/comments/post/:postId */
  getCommentsByPost(postId: string): Observable<BaseResponse<Comment[]>> {
    return this.http.get<BaseResponse<Comment[]>>(`${this.apiUrl}/post/${postId}`);
  }

  /** GET /api/comments/replies/:commentId */
  getReplies(commentId: string): Observable<BaseResponse<Comment[]>> {
    return this.http.get<BaseResponse<Comment[]>>(`${this.apiUrl}/replies/${commentId}`);
  }

  /** POST /api/comments — requires auth */
  addComment(request: CreateCommentRequest): Observable<BaseResponse<Comment>> {
    return this.http.post<BaseResponse<Comment>>(this.apiUrl, request);
  }

  /** Alias for addComment (compatibility) */
  createComment(request: CreateCommentRequest): Observable<BaseResponse<Comment>> {
    return this.addComment(request);
  }

  /** PUT /api/comments/:id — requires auth (own) */
  updateComment(id: string, content: string): Observable<BaseResponse<Comment>> {
    return this.http.put<BaseResponse<Comment>>(`${this.apiUrl}/${id}`, { content });
  }

  /** DELETE /api/comments/:id — requires auth (own or Admin) */
  deleteComment(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/${id}`);
  }

  /** PUT /api/comments/:id/approve — Admin or Author */
  approveComment(id: string): Observable<BaseResponse<string>> {
    return this.http.put<BaseResponse<string>>(`${this.apiUrl}/${id}/approve`, {});
  }

  /** PUT /api/comments/:id/reject — Admin or Author */
  rejectComment(id: string): Observable<BaseResponse<string>> {
    return this.http.put<BaseResponse<string>>(`${this.apiUrl}/${id}/reject`, {});
  }

  /** PUT /api/comments/:id/like — requires auth */
  likeComment(id: string): Observable<BaseResponse<string>> {
    return this.http.put<BaseResponse<string>>(`${this.apiUrl}/${id}/like`, {});
  }

  /** PUT /api/comments/:id/unlike — requires auth */
  unlikeComment(id: string): Observable<BaseResponse<string>> {
    return this.http.put<BaseResponse<string>>(`${this.apiUrl}/${id}/unlike`, {});
  }
}
