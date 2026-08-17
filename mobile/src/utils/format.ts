export const money = (n: number) => `${new Intl.NumberFormat('vi-VN').format(Math.round(n))} ₫`;
export const count = (n: number) => Math.round(n).toLocaleString('vi-VN');
export const percent = (n: number) => `${n.toFixed(1)}%`;
export const scoreOn100 = (n: number) => `${Math.round(n)}/100`;
