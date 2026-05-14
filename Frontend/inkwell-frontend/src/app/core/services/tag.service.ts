import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, Tag, CreateTagRequest } from '../models/models';

@Injectable({ providedIn: 'root' })
export class TagService {
  private apiUrl = `${environment.apiUrl}/tags`;

  constructor(private http: HttpClient) {}

  /** GET /api/tags — public */
  getAllTags(): Observable<BaseResponse<Tag[]>> {
    return this.http.get<BaseResponse<Tag[]>>(this.apiUrl);
  }

  /** GET /api/tags/trending?count=N — public */
  getTrendingTags(count: number = 8): Observable<BaseResponse<Tag[]>> {
    return this.http.get<BaseResponse<Tag[]>>(`${this.apiUrl}/trending?count=${count}`);
  }

  /** GET /api/tags/post/:postId — public */
  getTagsByPost(postId: string): Observable<BaseResponse<Tag[]>> {
    return this.http.get<BaseResponse<Tag[]>>(`${this.apiUrl}/post/${postId}`);
  }

  /** POST /api/tags — Admin or Author */
  createTag(dto: CreateTagRequest): Observable<BaseResponse<Tag>> {
    return this.http.post<BaseResponse<Tag>>(this.apiUrl, dto);
  }

  /** DELETE /api/tags/:id — Admin */
  deleteTag(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/${id}`);
  }

  /** POST /api/tags/add-to-post?postId&tagId — Admin, Author */
  addTagToPost(postId: string, tagId: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(
      `${this.apiUrl}/add-to-post?postId=${postId}&tagId=${tagId}`, {}
    );
  }

  /** DELETE /api/tags/remove-from-post?postId&tagId — Admin, Author */
  removeTagFromPost(postId: string, tagId: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(
      `${this.apiUrl}/remove-from-post?postId=${postId}&tagId=${tagId}`
    );
  }
}
