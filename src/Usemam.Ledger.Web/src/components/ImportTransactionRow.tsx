import { CATEGORIES } from "../constants/categories";

interface ImportTransaction {
  date: string;
  amount: number;
  description: string;
  category: string;
  isCredit: boolean;
  isDuplicate: boolean;
  isTransfer: boolean;
  selected: boolean;
}

interface ImportTransactionRowProps {
  transaction: ImportTransaction;
  index: number;
  onToggleSelect: (index: number) => void;
  onCategoryChange: (index: number, category: string) => void;
}

export function ImportTransactionRow({
  transaction,
  index,
  onToggleSelect,
  onCategoryChange,
}: ImportTransactionRowProps) {
  const formattedDate = new Date(transaction.date).toLocaleDateString();
  const formattedAmount = transaction.amount.toLocaleString("en-US", {
    style: "currency",
    currency: "USD",
  });

  return (
    <tr className={`import-row ${transaction.isDuplicate ? "duplicate" : ""}`}>
      <td className="import-cell checkbox-cell">
        <input
          type="checkbox"
          checked={transaction.selected}
          onChange={() => onToggleSelect(index)}
        />
      </td>
      <td className="import-cell date-cell">{formattedDate}</td>
      <td
        className={`import-cell amount-cell ${transaction.isCredit ? "credit" : "debit"}`}
      >
        {transaction.isCredit ? "+" : "-"}
        {formattedAmount}
      </td>
      <td className="import-cell description-cell" title={transaction.description}>
        {transaction.description}
        {transaction.isDuplicate && (
          <span className="duplicate-badge">Duplicate</span>
        )}
      </td>
      <td className="import-cell category-cell">
        {transaction.isTransfer ? (
          <span className="payment-label">Payment</span>
        ) : (
          <select
            value={transaction.category}
            onChange={(e) => onCategoryChange(index, e.target.value)}
            className="category-select"
          >
            {CATEGORIES.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </select>
        )}
      </td>
    </tr>
  );
}

// Mobile card version
export function ImportTransactionCard({
  transaction,
  index,
  onToggleSelect,
  onCategoryChange,
}: ImportTransactionRowProps) {
  const formattedDate = new Date(transaction.date).toLocaleDateString();
  const formattedAmount = transaction.amount.toLocaleString("en-US", {
    style: "currency",
    currency: "USD",
  });

  return (
    <div className={`import-card ${transaction.isDuplicate ? "duplicate" : ""}`}>
      <div className="import-card-header">
        <label className="import-card-checkbox">
          <input
            type="checkbox"
            checked={transaction.selected}
            onChange={() => onToggleSelect(index)}
          />
          <span className="import-card-date">{formattedDate}</span>
        </label>
        <span
          className={`import-card-amount ${transaction.isCredit ? "credit" : "debit"}`}
        >
          {transaction.isCredit ? "+" : "-"}
          {formattedAmount}
        </span>
      </div>
      <div className="import-card-description">
        {transaction.description}
        {transaction.isDuplicate && (
          <span className="duplicate-badge">Duplicate</span>
        )}
      </div>
      <div className="import-card-category">
        <label>Category:</label>
        {transaction.isTransfer ? (
          <span className="payment-label">Payment</span>
        ) : (
          <select
            value={transaction.category}
            onChange={(e) => onCategoryChange(index, e.target.value)}
            className="category-select"
          >
            {CATEGORIES.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </select>
        )}
      </div>
    </div>
  );
}
