/** 今天的本地日期，格式 YYYY-MM-DD（和後端 DateOnly、<input type="date"> 相同）。 */
export function todayIso(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}

/** 後端回傳 "08:00:00"，畫面上只顯示 "08:00"。 */
export function shortTime(time: string): string {
  return time.slice(0, 5);
}
