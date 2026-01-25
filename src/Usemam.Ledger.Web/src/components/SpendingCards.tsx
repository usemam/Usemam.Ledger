import type { SpendingReportDto } from "../types/api";

const MONTHS = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

interface SpendingCardsProps {
  report: SpendingReportDto;
}

function formatMoney(value: number): string {
  return value.toLocaleString("en-US", {
    style: "currency",
    currency: "USD",
    minimumFractionDigits: 2,
  });
}

function getValueClass(value: number): string {
  if (value === 0) return "spending-value-zero";
  return value > 0 ? "spending-value-positive" : "spending-value-negative";
}

export function SpendingCards({ report }: SpendingCardsProps) {
  return (
    <div className="spending-cards">
      {report.categories.map((category) => (
        <div key={category.category} className="category-card">
          <div className="category-card-header">
            <span className="category-name">{category.category}</span>
          </div>
          <div className="category-card-total">
            <span className="total-label">Year Total:</span>
            <span className={getValueClass(category.yearTotal)}>
              {formatMoney(category.yearTotal)}
            </span>
          </div>
          <div className="category-monthly-grid">
            {category.monthlyAmounts.map((amount, idx) => (
              <div key={idx} className="monthly-item">
                <span className="month-name">{MONTHS[idx].slice(0, 3)}</span>
                <span className={getValueClass(amount)}>
                  {formatMoney(amount)}
                </span>
              </div>
            ))}
          </div>
        </div>
      ))}

      <div className="category-card category-card-totals">
        <div className="category-card-header">
          <span className="category-name">Net Totals</span>
        </div>
        <div className="category-card-total">
          <span className="total-label">Year Net:</span>
          <span className={getValueClass(report.yearlyNet)}>
            {formatMoney(report.yearlyNet)}
          </span>
        </div>
        <div className="category-monthly-grid">
          {report.monthlyTotals.map((total, idx) => (
            <div key={idx} className="monthly-item">
              <span className="month-name">{MONTHS[idx].slice(0, 3)}</span>
              <span className={getValueClass(total)}>{formatMoney(total)}</span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
