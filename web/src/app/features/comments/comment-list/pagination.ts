/** Номера страниц для переключателя: окно из `size` страниц вокруг текущей. */
export function visiblePages(current: number, total: number, size = 5): number[] {
  if (total <= 0) {
    return [];
  }

  const count = Math.min(size, total);
  let start = Math.max(1, current - Math.floor(count / 2));
  start = Math.min(start, total - count + 1);

  return Array.from({ length: count }, (_, i) => start + i);
}