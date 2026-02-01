import { useEffect, useRef } from "react";
import type { TransactionDto } from "../types/api";
import { TransactionItem, TransactionCard } from "./TransactionItem";

interface TransactionListProps {
  transactions: TransactionDto[];
  isLoading?: boolean;
  error?: Error | null;
  isFetchingNextPage?: boolean;
  hasNextPage?: boolean;
  onLoadMore?: () => void;
}

export function TransactionList({
  transactions,
  isLoading,
  error,
  isFetchingNextPage,
  hasNextPage,
  onLoadMore,
}: TransactionListProps) {
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
