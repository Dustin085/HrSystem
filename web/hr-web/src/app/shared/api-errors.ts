import { HttpErrorResponse } from '@angular/common/http';

import { ValidationProblemDetails } from '../api';

/**
 * 把後端回傳的 ProblemDetails / ValidationProblemDetails 轉成可以直接顯示的訊息。
 * 後端的 400（欄位錯誤）和 409（排班重疊）都是這個格式。
 */
export function toErrorMessages(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse)) {
    return ['發生未預期的錯誤。'];
  }

  if (error.status === 0) {
    return ['無法連線到伺服器，請確認後端是否已啟動。'];
  }

  const body = error.error as ValidationProblemDetails | null;

  if (body?.errors) {
    return Object.values(body.errors).flat();
  }

  if (body?.detail) {
    return [body.detail];
  }

  if (body?.title) {
    return [body.title];
  }

  return [`伺服器錯誤（${error.status}）。`];
}
