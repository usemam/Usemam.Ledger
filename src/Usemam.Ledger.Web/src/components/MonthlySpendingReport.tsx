import { useState } from "react";
import { useSpendingReport } from "../hooks/useSpendingReport";
import { YearNavigator } from "./YearNavigator";
import { SpendingTable } from "./SpendingTable";
import { SpendingCards } from "./SpendingCards";

export function MonthlySpendingReport() {
  const [year, setYear] = useState(() => new Date().getFullYear());
  const { data: report, isLoading, error } = useSpendingReport(year);

  if (isLoading) {
    return <div className="loading">Loading spending report...</div>;
  }

  if (error) {
    return (
      <div className="error">
        Error loading report: {error instanceof Error ? error.message : "Unknown error"}
      </div>
    );
  }

  if (!report) {
    return <div className="empty">No data available</div>;
  }

  return (
    <div className="spending-report">
      <YearNavigator year={year} onYearChange={setYear} />

      {report.categories.length === 0 ? (
        <div className="empty">No transactions found for {year}</div>
      ) : (
        <>
          <div className="hide-mobile">
            <SpendingTable report={report} />
          </div>
          <div className="hide-desktop">
            <SpendingCards report={report} />
          </div>
        </>
      )}
    </div>
  );
}
