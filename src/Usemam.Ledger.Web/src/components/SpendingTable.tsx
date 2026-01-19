import type { SpendingReportDto } from "../types/api";
import { SpendingCell } from "./SpendingCell";

const MONTHS = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

interface SpendingTableProps {
  report: SpendingReportDto;
}

export function SpendingTable({ report }: SpendingTableProps) {
  return (
    <div className="spending-table-wrapper">
      <table className="spending-table">
        <thead>
          <tr>
            <th>Category</th>
            {MONTHS.map((month) => (
              <th key={month}>{month}</th>
            ))}
            <th>Total</th>
          </tr>
        </thead>
        <tbody>
          {report.categories.map((category) => (
            <tr key={category.category}>
              <td className="spending-category-name">{category.category}</td>
              {category.monthlyAmounts.map((amount, idx) => (
                <SpendingCell key={idx} value={amount} />
              ))}
              <SpendingCell value={category.yearTotal} />
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="spending-totals-row">
            <td className="spending-category-name">Net Total</td>
            {report.monthlyTotals.map((total, idx) => (
              <SpendingCell key={idx} value={total} isNetTotal />
            ))}
            <SpendingCell value={report.yearlyNet} isNetTotal />
          </tr>
        </tfoot>
      </table>
    </div>
  );
}
