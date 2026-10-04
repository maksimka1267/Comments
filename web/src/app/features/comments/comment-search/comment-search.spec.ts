import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  TestRequest,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CommentSearch } from './comment-search';
import { CommentDto, SearchHit } from '../../../core/models';

function makeHit(id: string, userName = 'Anna', parentId: string | null = null): SearchHit {
  return {
    id,
    parentId,
    userName,
    snippet: `snippet of ${id}`,
    createdAt: '2026-09-30T20:49:52Z',
  };
}

describe('CommentSearch', () => {
  let fixture: ComponentFixture<CommentSearch>;
  let http: HttpTestingController;
  let element: HTMLElement;

  // вводит запрос и отправляет форму, как это делает пользователь по Enter
  function submit(text: string): void {
    element.querySelector<HTMLInputElement>('input')!.value = text;
    element.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();
  }

  // ловит запрос поиска и проверяет его параметры
  function expectSearch(q: string, page = 1): TestRequest {
    const req = http.expectOne((r) => r.url === '/api/comments/search');
    expect(req.request.params.get('q')).toBe(q);
    expect(req.request.params.get('page')).toBe(String(page));
    return req;
  }

  function flushHits(req: TestRequest, items: SearchHit[], totalPages = 1, page = 1): void {
    req.flush({ items, page, pageSize: 25, totalCount: items.length, totalPages });
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentSearch],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CommentSearch);
    element = fixture.nativeElement;
    fixture.detectChanges();
  });

  it('does not search for a blank query', () => {
    submit('   ');

    http.expectNone((r) => r.url === '/api/comments/search');
    expect(element.querySelector('.search-results')).toBeNull();
  });

  it('searches with the trimmed query and shows the hits', () => {
    submit('  привет ');

    flushHits(expectSearch('привет'), [makeHit('a'), makeHit('b', 'Bob', 'a')]);

    expect(element.querySelectorAll('.search-hit').length).toBe(2);
    expect(element.textContent).toContain('snippet of a');
    expect(element.querySelectorAll('.search-reply-badge').length).toBe(1);
  });

  it('says so when nothing was found', () => {
    submit('nothing');

    flushHits(expectSearch('nothing'), []);

    expect(element.textContent).toContain('Ничего не найдено');
  });

  it('shows an error when search is unavailable', () => {
    submit('x');

    expectSearch('x').flush({}, { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();

    expect(element.querySelector('[role="alert"]')?.textContent).toContain('временно недоступен');
  });

  it('requests the chosen page', () => {
    submit('x');
    flushHits(expectSearch('x'), [makeHit('a')], 3);

    element.querySelector<HTMLButtonElement>('[data-search-page="2"]')!.click();

    expectSearch('x', 2);
  });

  it('loads the full comment on demand', () => {
    submit('x');
    flushHits(expectSearch('x'), [makeHit('a')]);

    element.querySelector<HTMLButtonElement>('.search-more')!.click();

    const full: CommentDto = {
      id: 'a',
      parentId: null,
      userName: 'Anna',
      email: 'anna@example.com',
      homePage: null,
      text: '<strong>full</strong> text',
      createdAt: '2026-09-30T20:49:52Z',
      attachment: null,
      replies: [],
    };

    http.expectOne('/api/comments/a').flush(full);
    fixture.detectChanges();

    expect(element.querySelector('.search-full')?.textContent).toContain('full text');
  });

  it('closes the results on reset', () => {
    submit('x');
    flushHits(expectSearch('x'), [makeHit('a')]);

    element.querySelector<HTMLButtonElement>('.search-reset')!.click();
    fixture.detectChanges();

    expect(element.querySelector('.search-results')).toBeNull();
    expect(element.querySelector<HTMLInputElement>('input')!.value).toBe('');
  });
});