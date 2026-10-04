import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CaptchaResponse,
  CommentDto,
  PagedResult,
  SearchHit,
  SortDir,
  SortField,
} from './models';

@Injectable({
  providedIn: 'root',
})
export class CommentsApi {
  private readonly http = inject(HttpClient);

  list(
    sortBy: SortField,
    sortDir: SortDir,
    page: number,
  ): Observable<PagedResult<CommentDto>> {
    const params = new HttpParams()
      .set('sortBy', sortBy)
      .set('sortDir', sortDir)
      .set('page', page);

    return this.http.get<PagedResult<CommentDto>>(
      '/api/comments',
      { params },
    );
  }

  getById(id: string): Observable<CommentDto> {
    return this.http.get<CommentDto>(
      `/api/comments/${id}`,
    );
  }
  search(
    query: string,
    page: number,
  ): Observable<PagedResult<SearchHit>> {
    const params = new HttpParams()
      .set('q', query)
      .set('page', page);

    return this.http.get<PagedResult<SearchHit>>(
      '/api/comments/search',
      { params },
    );
  }
  getCaptcha(): Observable<CaptchaResponse> {
    return this.http.get<CaptchaResponse>(
      '/api/captcha',
    );
  }

  create(
    userName: string,
    email: string,
    homePage: string | null,
    text: string,
    parentId: string | null,
    captchaId: string,
    captchaAnswer: string,
    file?: File | null,
  ): Observable<CommentDto> {
    const formData = new FormData();

    formData.append('UserName', userName);
    formData.append('Email', email);
    formData.append('Text', text);
    formData.append('CaptchaId', captchaId);
    formData.append('CaptchaAnswer', captchaAnswer);

    if (homePage) {
      formData.append('HomePage', homePage);
    }

    if (parentId) {
      formData.append('ParentId', parentId);
    }

    if (file) {
      formData.append('file', file, file.name);
    }

    return this.http.post<CommentDto>(
      '/api/comments',
      formData,
    );
  }
}