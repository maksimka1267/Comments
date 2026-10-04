import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';

import { CommentList } from './comment-list';
import { CommentDto } from '../../../core/models';
import { CommentCreatedNotification, CommentsRealtime } from '../../../core/comments-realtime';

// заглушка: настоящее подключение к хабу в тестах не нужно
class FakeRealtime {
  readonly created = new Subject<CommentCreatedNotification>();
  readonly resync = new Subject<void>();

  readonly commentCreated$ = this.created.asObservable();
  readonly resync$ = this.resync.asObservable();

  started = false;
  stopped = false;

  start(): void {
    this.started = true;
  }

  stop(): void {
    this.stopped = true;
  }
}

function makeComment(id: string, userName: string, replies: CommentDto[] = []): CommentDto {
  return {
    id,
    parentId: 'x',
    userName,
    email: `${userName}@example.com`,
    homePage: null,
    text: `text of ${userName}`,
    createdAt: '2026-09-30T20:49:52Z',
    attachment: null,
    replies,
  };
}

describe('CommentList', () => {
  let fixture: ComponentFixture<CommentList>;
  let http: HttpTestingController;
  let realtime: FakeRealtime;

  // ловит ближайший запрос списка, проверяет параметры и отвечает пустой страницей
  function expectList(sortBy: string, sortDir: string, page = 1, totalPages = 0): void {
    const req = http.expectOne((r) => r.url === '/api/comments');
    expect(req.request.params.get('sortBy')).toBe(sortBy);
    expect(req.request.params.get('sortDir')).toBe(sortDir);
    expect(req.request.params.get('page')).toBe(String(page));

    req.flush({ items: [], page, pageSize: 25, totalCount: totalPages * 25, totalPages });
    fixture.detectChanges();
  }

  // отвечает на ближайший запрос списка заданными комментариями
  function flushList(items: CommentDto[]): void {
    const req = http.expectOne((r) => r.url === '/api/comments');
    req.flush({ items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 });
    fixture.detectChanges();
  }

  function clickSort(field: string): void {
    fixture.nativeElement.querySelector(`[data-sort="${field}"]`).click();
  }

  function clickPage(target: string): void {
    fixture.nativeElement.querySelector(`[data-page="${target}"]`).click();
  }

  function banner(): HTMLElement | null {
    return fixture.nativeElement.querySelector('.new-comments');
  }

  beforeEach(async () => {
    realtime = new FakeRealtime();

    await TestBed.configureTestingModule({
      imports: [CommentList],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: CommentsRealtime, useValue: realtime },
      ],
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
    const top = {
      ...makeComment('1', 'Anna'),
      parentId: null,
      replies: [makeComment('2', 'Bob', [makeComment('3', 'Carol')])],
    };

    flushList([top]);

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelectorAll('app-reply-tree .reply').length).toBe(2);
    expect(element.querySelector('app-reply-tree')?.textContent).toContain('Carol');
  });

  describe('real-time updates', () => {
    // на экране: комментарий "a" и его ответ "a1"
    beforeEach(() => {
      flushList([makeComment('a', 'Anna', [makeComment('a1', 'Bob')])]);
    });

    it('starts the connection on init and stops it on destroy', () => {
      expect(realtime.started).toBe(true);

      fixture.destroy();

      expect(realtime.stopped).toBe(true);
    });

    it('shows no banner until something happens', () => {
      expect(banner()).toBeNull();
    });

    it('shows the banner for a new top-level comment', () => {
      realtime.created.next({ commentId: 'b', parentId: null });
      fixture.detectChanges();

      expect(banner()).not.toBeNull();
    });

    it('ignores comments that are already on screen', () => {
      realtime.created.next({ commentId: 'a', parentId: null });
      realtime.created.next({ commentId: 'a1', parentId: 'a' });
      fixture.detectChanges();

      expect(banner()).toBeNull();
    });

    it('ignores a reply whose parent is not on screen', () => {
      realtime.created.next({ commentId: 'x', parentId: 'not-on-screen' });
      fixture.detectChanges();

      expect(banner()).toBeNull();
    });

    it('shows the banner for a reply to a visible comment', () => {
      realtime.created.next({ commentId: 'y', parentId: 'a1' });
      fixture.detectChanges();

      expect(banner()).not.toBeNull();
    });

    it('shows the banner after the connection was restored', () => {
      realtime.resync.next();
      fixture.detectChanges();

      expect(banner()).not.toBeNull();
    });

    it('reloads the list and hides the banner on click', () => {
      realtime.created.next({ commentId: 'b', parentId: null });
      fixture.detectChanges();

      banner()!.click();
      flushList([makeComment('b', 'Carol'), makeComment('a', 'Anna')]);

      expect(banner()).toBeNull();
    });
  });
});