import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentList } from './comment-list';

describe('CommentList', () => {
  let fixture: ComponentFixture<CommentList>;
  let http: HttpTestingController;

  const emptyPage = { items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 };

  // ловит ближайший запрос списка, проверяет параметры и отвечает пустой страницей
  function expectList(sortBy: string, sortDir: string): void {
    const req = http.expectOne((r) => r.url === '/api/comments');
    expect(req.request.params.get('sortBy')).toBe(sortBy);
    expect(req.request.params.get('sortDir')).toBe(sortDir);
    req.flush(emptyPage);
  }

  function clickSort(field: string): void {
    fixture.nativeElement.querySelector(`[data-sort="${field}"]`).click();
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
});