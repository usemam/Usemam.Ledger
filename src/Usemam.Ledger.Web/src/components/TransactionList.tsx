import { useEffect, useRef } from "react";
import type { TransactionDto } from "../types/api";
import { TransactionItem, TransactionCard } from "./TransactionItem";

interface TransactionListProps {
  transactions: TransactionDto[];
  accountName?: string;
  accountBalance?: number;
  isLoading?: boolean;
  error?: Error | null;
  isFetchingNextPage?: boolean;
  hasNextPage?: boolean;
  onLoadMore?: () => void;
}

function computeRunningBalances(
  transactions: TransactionDto[],
  accountName: string,
  currentBalance: number
): number[] {
  const balances: number[] = [];
  let balance = currentBalance;
  for (const t of transactions) {
    balances.push(balance);
    const amount = t.amount.amount;
    if (t.type === "Credit") {
      balance -= amount;
    } else if (t.type === "Debit") {
      balance += amount;
    } else if (t.type === "Transfer") {
      if (t.destinationAccount === accountName) {
        balance -= amount;
      } else {
        balance += amount;
      }
    }
  }
  return balances;
}

export function TransactionList({
  transactions,
  accountName,
  accountBalance,
  isLoading,
  error,
  isFetchingNextPage,
  hasNextPage,
  onLoadMore,
}: TransactionListProps) {
  const runningBalances =
    accountName !== undefined && accountBalance !== undefined
      ? computeRunningBalances(transactions, accountName, accountBalance)
      : null;
  const loadMoreRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!loadMoreRef.current || !onLoadMore || !hasNextPage) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          onLoadMore();
        }
      },
      { threshold: 0.1 }
    );

    observer.observe(loadMoreRef.current);

    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, onLoadMore]);

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
            <th className="amount">Balance</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((transaction, index) => (
            <TransactionItem
              key={index}
              transaction={transaction}
              runningBalance={runningBalances?.[index]}
            />
          ))}
        </tbody>
      </table>

      {/* Mobile Card View */}
      <div className="transaction-cards">
        {transactions.map((transaction, index) => (
          <TransactionCard
            key={index}
            transaction={transaction}
            runningBalance={runningBalances?.[index]}
          />
        ))}
      </div>

      {/* Load More Trigger */}
      <div ref={loadMoreRef} className="load-more-trigger">
        {isFetchingNextPage && (
          <div className="loading-more">Loading more transactions...</div>
        )}
        {!hasNextPage && transactions.length > 0 && (
          <div className="no-more-transactions">No more transactions</div>
        )}
      </div>
    </div>
  );
}
