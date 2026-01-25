import { useState } from "react";
import { AccountList } from "./AccountList";
import { ReportsPage } from "./ReportsPage";

type Tab = "accounts" | "reports";

export function MainPage() {
  const [activeTab, setActiveTab] = useState<Tab>("accounts");

  return (
    <div className="main-page">
      <div className="tab-nav">
        <button
          className={`tab-button ${activeTab === "accounts" ? "active" : ""}`}
          onClick={() => setActiveTab("accounts")}
        >
          Accounts
        </button>
        <button
          className={`tab-button ${activeTab === "reports" ? "active" : ""}`}
          onClick={() => setActiveTab("reports")}
        >
          Reports
        </button>
      </div>
      <div className="tab-content">
        {activeTab === "accounts" && <AccountList />}
        {activeTab === "reports" && <ReportsPage />}
      </div>
    </div>
  );
}
