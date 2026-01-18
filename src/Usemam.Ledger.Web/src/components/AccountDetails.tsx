import { useParams, Link } from "react-router-dom";
import { useAccount } from "../hooks/useAccounts";
import { useAccountTransactions } from "../hooks/useTransactions";
import { Money } from "./Money";
import { TransactionList } from "./TransactionList";

export function AccountDetails() {
  const { name } = useParams<{ name: string }>();
  const decodedName = name ? decodeURIComponent(name) : "";

  const {
    data: account,
    isLoading: accountLoading,
    error: accountError,
  } = useAccount(decodedName);

  const {
    data: transactions,
    isLoading: transactionsLoading,
    error: transactionsError,
  } = useAccountTransactions(decodedName);

  if (accountLoading) {
    return <div className="loading">Loading account...</div>;
  }

  if (accountError) {
    return (
      <div className="error">Error loading account: {accountError.message}</div>
    );
  }

  if (!account) {
    return <div className="error">Account not found</div>;
  }

  return (
    <div className="account-details">
      <Link to="/" className="back-link">
        &larr; Back to Accounts
      </Link>

      <h2>{account.name}</h2>

      <div className="account-info">
        <div className="info-row">
          <span className="label">Balance:</span>
          <Money money={account.balance} />
        </div>
        {account.creditLimit.amount > 0 && (
          <div className="info-row">
            <span className="label">Credit Limit:</span>
            <Money money={account.creditLimit} />
          </div>
        )}
        <div className="info-row">
          <span className="label">Created:</span>
          <span>{new Date(account.created).toLocaleDateString()}</span>
        </div>
        <div className="info-row">
          <span className="label">Status:</span>
          <span className={account.isClosed ? "status-closed" : "status-open"}>
            {account.isClosed ? "Closed" : "Open"}
          </span>
        </div>
      </div>

      <div className="transaction-section">
        <h3>Transactions</h3>
        <TransactionList
          transactions={transactions || []}
          isLoading={transactionsLoading}
          error={transactionsError}
        />
      </div>
    </div>
  );
}
