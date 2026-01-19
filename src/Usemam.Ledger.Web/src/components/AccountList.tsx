import { useAccounts } from "../hooks/useAccounts";
import { AccountCard } from "./AccountCard";

export function AccountList() {
  const { data: accounts, isLoading, error } = useAccounts();

  if (isLoading) {
    return <div className="loading">Loading accounts...</div>;
  }

  if (error) {
    return (
      <div className="error">Error loading accounts: {error.message}</div>
    );
  }

  if (!accounts || accounts.length === 0) {
    return <div className="empty">No accounts found</div>;
  }

  return (
    <div className="account-list">
      <div className="account-grid">
        {accounts.map((account) => (
          <AccountCard key={account.name} account={account} />
        ))}
      </div>
    </div>
  );
}
