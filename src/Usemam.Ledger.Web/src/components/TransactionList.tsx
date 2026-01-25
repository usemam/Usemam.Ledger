import type { TransactionDto } from "../types/api";
import { TransactionItem, TransactionCard } from "./TransactionItem";

interface TransactionListProps {
  transactions: TransactionDto[];
  isLoading?: boolean;
  error?: Error | null;
}

export function TransactionList({
  transactions,
  isLoading,
  error,
}: TransactionListProps) {
  if (isLoading) {
    return <div className="loading">Loading transactions...</div>;
  }

  if (error) {
    return (
      <div className="error">Error loading transactions: {error.message}</div>
    );
  }

  if (!transactions || transactions.length === 0) {
    return <div className="empty">No transactions found</div>;
  }

  return (
    <div className="transaction-list">
      {/* Desktop Table View */}
      <table className="transaction-table">
        <thead>
          <tr>
            <th>Date</th>
            <th>Type</th>
            <th>Description</th>
            <th className="amount">Amount</th>
            <th>Notes</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((transaction, index) => (
            <TransactionItem key={index} transaction={transaction} />
          ))}
        </tbody>
      </table>

      {/* Mobile Card View */}
      <div className="transaction-cards">
        {transactions.map((transaction, index) => (
          <TransactionCard key={index} transaction={transaction} />
        ))}
      </div>
    </div>
  );
}
