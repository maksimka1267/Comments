export type AttachmentKind = 'Image' | 'Text';

export interface Attachment {
  id: string;
  kind: AttachmentKind;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export interface CommentDto {
  id: string;
  parentId: string | null;
  userName: string;
  email: string;
  homePage: string | null;
  text: string;
  createdAt: string;
  attachment: Attachment | null;
  replies: CommentDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export type SortField = 'date' | 'userName' | 'email';
export type SortDir = 'asc' | 'desc';