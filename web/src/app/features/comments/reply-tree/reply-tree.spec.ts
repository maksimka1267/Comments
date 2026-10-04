import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentDto } from '../../../core/models';
import { ReplyTree } from './reply-tree';

function comment(id: string, userName: string, replies: CommentDto[] = []): CommentDto {
  return {
    id,
    parentId: null,
    userName,
    email: `${userName}@example.com`,
    homePage: null,
    text: `text of ${userName}`,
    createdAt: '2026-09-30T20:49:52Z',
    attachment: null,
    replies,
  };
}

describe('ReplyTree', () => {
  let fixture: ComponentFixture<ReplyTree>;

  function render(replies: CommentDto[]): HTMLElement {
    fixture = TestBed.createComponent(ReplyTree);
    fixture.componentRef.setInput('replies', replies);
    fixture.detectChanges();
    return fixture.nativeElement;
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ReplyTree] }).compileComponents();
  });

  it('renders every reply of a flat list', () => {
    const root = render([comment('1', 'Bob'), comment('2', 'Carol')]);

    expect(root.querySelectorAll('.reply').length).toBe(2);
  });

  it('renders nested replies inside their parent', () => {
    const root = render([comment('1', 'Bob', [comment('2', 'Carol', [comment('3', 'Dave')])])]);

    expect(root.querySelectorAll('.reply').length).toBe(3);
    expect(root.querySelector('.reply .reply .reply')?.textContent).toContain('Dave');
  });

  it('shows the text of a reply as HTML', () => {
    const reply = { ...comment('1', 'Bob'), text: 'a <strong>bold</strong> word' };
    const root = render([reply]);

    expect(root.querySelector('.text strong')?.textContent).toBe('bold');
  });

  it('stops indenting very deep branches', () => {
    // цепочка из 7 ответов: уровни 0..6
    let chain = comment('7', 'u7');
    for (let i = 6; i >= 1; i--) {
      chain = comment(String(i), `u${i}`, [chain]);
    }

    const root = render([chain]);

    expect(root.querySelectorAll('.reply').length).toBe(7);
    expect(root.querySelectorAll('.reply.flat').length).toBe(2); // уровни 5 и 6
  });
});