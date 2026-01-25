import type { TransactionDto } from "../types/api";
import { Money } from "./Money";

interface TransactionItemProps {
  transaction: TransactionDto;
}

function getTransactionDescription(transaction: TransactionDto): string {
  switch (transaction.type) {
    case "Transfer":
      return `Transfer from ${transaction.sourceAccount} to ${transaction.destinationAccount}`;
    case "Credit":
      return `Credit from ${transaction.creditSource} to ${transaction.destinationAccount}`;
    case "Debit":
      return `Debit from ${transaction.sourceAccount} to ${transaction.debitTarget}`;
    default:
      return "Unknown transaction";
  }
}

// Desktop Table Row Component
export function TransactionItem({ transaction }: TransactionItemProps) {
  return (
    <tr className={`transaction-row transaction-${transaction.type.toLowerCase()}`}>
      <td>{new Date(transaction.date).toLocaleDateString()}</td>
      <td>{transaction.type}</td>
      <td>{getTransactionDescription(transaction)}</td>
      <td className="amount">
        <Money money={transaction.amount} />
      </td>
      <td>{transaction.description || "-"}</td>
    </tr>
  );
}

// Mobile Card Component
export function TransactionCard({ transaction }: TransactionItemProps) {
  const typeClass = transaction.type.toLowerCase();

  return (
    <div className={`transaction-card transaction-${typeClass}`}>
      <div className="transaction-card-header">
        <span className="transaction-card-date">
          {new Date(transaction.date).toLocaleDateString()}
        </span>
        <span className={`transaction-card-type ${typeClass}`}>
          {transaction.type}
        </span>
      </div>
      <div className="transaction-card-description">
        {getTransactionDescription(transaction)}
      </div>
      <div className="transaction-card-footer">
        <span className="transaction-card-amount">
          <Money money={transaction.amount} />
        </span>
        {transaction.description && (
          <span className="transaction-card-notes">{transaction.description}</span>
        )}
      </div>
    </div>
  );
}
