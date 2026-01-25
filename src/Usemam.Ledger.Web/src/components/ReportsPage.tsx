import { useState } from "react";
import { MonthlySpendingReport } from "./MonthlySpendingReport";

type ReportView = "none" | "spending";

export function ReportsPage() {
  const [selectedReport, setSelectedReport] = useState<ReportView>("none");

  if (selectedReport === "spending") {
    return (
      <div className="report-view">
        <button className="back-link" onClick={() => setSelectedReport("none")}>
          ← Back to Reports
        </button>
        <MonthlySpendingReport />
      </div>
    );
  }

  return (
    <div className="reports-grid">
      <div className="report-card" onClick={() => setSelectedReport("spending")}>
        <h3>Spending Report</h3>
        <p className="report-card-description">
          View income and expenses by category for each month
        </p>
      </div>
    </div>
  );
}
