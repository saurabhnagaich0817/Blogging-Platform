import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, Category, CreateCategoryRequest, Tag } from '../models/models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private apiUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient) {}

  getAllCategories(): Observable<BaseResponse<Category[]>> {
    return this.http.get<BaseResponse<Category[]>>(`${this.apiUrl}/categories`);
  }

  createCategory(request: CreateCategoryRequest): Observable<BaseResponse<Category>> {
    return this.http.post<BaseResponse<Category>>(`${this.apiUrl}/categories`, request);
  }

  deleteCategory(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/categories/${id}`);
  }

  getAllTags(): Observable<BaseResponse<Tag[]>> {
    return this.http.get<BaseResponse<Tag[]>>(`${this.apiUrl}/tags`);
  }

  getTrendingTags(count: number = 10): Observable<BaseResponse<Tag[]>> {
    return this.http.get<BaseResponse<Tag[]>>(`${this.apiUrl}/tags/trending?count=${count}`);
  }

  createTag(name: string): Observable<BaseResponse<Tag>> {
    return this.http.post<BaseResponse<Tag>>(`${this.apiUrl}/tags`, { name });
  }
}
