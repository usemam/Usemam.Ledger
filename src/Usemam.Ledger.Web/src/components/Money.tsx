import type { MoneyDto } from "../types/api";

interface MoneyProps {
  money: MoneyDto;
  showSign?: boolean;
}

export function Money({ money, showSign }: MoneyProps) {
  const formatted = new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: money.currency,
  }).format(money.amount);

  const colorClass = money.amount >= 0 ? "money-positive" : "money-negative";
  const displayValue = showSign && money.amount > 0 ? `+${formatted}` : formatted;

  return <span className={`money ${colorClass}`}>{displayValue}</span>;
}
