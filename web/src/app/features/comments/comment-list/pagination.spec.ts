import { visiblePages } from './pagination';

describe('visiblePages', () => {
  it('returns nothing when there are no pages', () => {
    expect(visiblePages(1, 0)).toEqual([]);
  });

  it('returns all pages when there are few', () => {
    expect(visiblePages(1, 3)).toEqual([1, 2, 3]);
  });

  it('centers the window on the current page', () => {
    expect(visiblePages(5, 10)).toEqual([3, 4, 5, 6, 7]);
  });

  it('sticks to the start', () => {
    expect(visiblePages(1, 10)).toEqual([1, 2, 3, 4, 5]);
  });

  it('sticks to the end', () => {
    expect(visiblePages(10, 10)).toEqual([6, 7, 8, 9, 10]);
  });
});