import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentList } from './comment-list';
import { CommentDto } from '../../../core/models';

describe('CommentList', () => {
  let fixture: ComponentFixture<CommentList>;
  let http: HttpTestingController;

  // ловит ближайший запрос списка, проверяет параметры и отвечает пустой страницей
  function expectList(sortBy: string, sortDir: string, page = 1, totalPages = 0): void {
    const req = http.expectOne((r) => r.url === '/api/comments');
    expect(req.request.params.get('sortBy')).toBe(sortBy);
    expect(req.request.params.get('sortDir')).toBe(sortDir);
    expect(req.request.params.get('page')).toBe(String(page));

    req.flush({ items: [], page, pageSize: 25, totalCount: totalPages * 25, totalPages });
    fixture.detectChanges();
  }

  function clickSort(field: string): void {
    fixture.nativeElement.querySelector(`[data-sort="${field}"]`).click();
  }

  function clickPage(target: string): void {
    fixture.nativeElement.querySelector(`[data-page="${target}"]`).click();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentList],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(CommentList);
    fixture.detectChanges();
  });

  it('loads newest comments first by default', () => {
    expectList('date', 'desc');
  });

  it('sorts a new column ascending, then toggles direction', () => {
    expectList('date', 'desc');

    clickSort('userName');
    expectList('userName', 'asc');

    clickSort('userName');
    expectList('userName', 'desc');
  });

  it('returns to newest first when the date column is chosen', () => {
    expectList('date', 'desc');

    clickSort('email');
    expectList('email', 'asc');

    clickSort('date');
    expectList('date', 'desc');
  });

  it('hides the pager when everything fits on one page', () => {
    expectList('date', 'desc', 1, 1);

    expect(fixture.nativeElement.querySelector('.pager')).toBeNull();
  });

  it('requests the chosen page', () => {
    expectList('date', 'desc', 1, 3);

    clickPage('2');
    expectList('date', 'desc', 2, 3);
  });

  it('moves forward and back with the arrow buttons', () => {
    expectList('date', 'desc', 1, 3);

    clickPage('next');
    expectList('date', 'desc', 2, 3);

    clickPage('prev');
    expectList('date', 'desc', 1, 3);
  });

  it('returns to the first page when sorting changes', () => {
    expectList('date', 'desc', 1, 3);

    clickPage('3');
    expectList('date', 'desc', 3, 3);

    clickSort('email');
    expectList('email', 'asc', 1, 3);
  });
  it('shows replies under their top-level comment', () => {
  const reply = (id: string, userName: string, replies: CommentDto[] = []): CommentDto => ({
    id,
    parentId: 'x',
    userName,
    email: `${userName}@example.com`,
    homePage: null,
    text: `text of ${userName}`,
    createdAt: '2026-09-30T20:49:52Z',
    attachment: null,
    replies,
  });

  const top = { ...reply('1', 'Anna'), parentId: null, replies: [reply('2', 'Bob', [reply('3', 'Carol')])] };

  const req = http.expectOne((r) => r.url === '/api/comments');
  req.flush({ items: [top], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 });
  fixture.detectChanges();

  const element: HTMLElement = fixture.nativeElement;
  expect(element.querySelectorAll('app-reply-tree .reply').length).toBe(2);
  expect(element.querySelector('.replies-row')?.textContent).toContain('Carol');
});
});