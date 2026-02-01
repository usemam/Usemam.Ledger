import { CATEGORIES } from "../constants/categories";
import type { AccountDto } from "../types/api";

interface ImportTransaction {
  date: string;
  amount: number;
  description: string;
  category: string;
  isCredit: boolean;
  isDuplicate: boolean;
  isTransfer: boolean;
  transferAccountName: string | null;
  selected: boolean;
}

interface ImportTransactionRowProps {
  transaction: ImportTransaction;
  index: number;
  accounts: AccountDto[];
  currentAccountName: string;
  onToggleSelect: (index: number) => void;
  onCategoryChange: (index: number, category: string) => void;
  onTransferAccountChange: (index: number, accountName: string | null) => void;
  onToggleTransfer: (index: number) => void;
}

export function ImportTransactionRow({
  transaction,
  index,
  accounts,
  currentAccountName,
  onToggleSelect,
  onCategoryChange,
  onTransferAccountChange,
  onToggleTransfer,
}: ImportTransactionRowProps) {
  const formattedDate = new Date(transaction.date).toLocaleDateString();
  const formattedAmount = transaction.amount.toLocaleString("en-US", {
    style: "currency",
    currency: "USD",
  });

  // Filter out the current account from the transfer dropdown
  const availableAccounts = accounts.filter(a => a.name !== currentAccountName);

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
          <div className="transfer-select-container">
            <select
              value={transaction.transferAccountName || ""}
              onChange={(e) => onTransferAccountChange(index, e.target.value || null)}
              className="category-select transfer-account-select"
            >
              <option value="">Select account...</option>
              {availableAccounts.map((account) => (
                <option key={account.name} value={account.name}>
                  {account.name}
                </option>
              ))}
            </select>
            <button
              type="button"
              className="btn-unmark-transfer"
              onClick={() => onToggleTransfer(index)}
              title="Not a transfer"
            >
              ✕
            </button>
          </div>
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
  accounts,
  currentAccountName,
  onToggleSelect,
  onCategoryChange,
  onTransferAccountChange,
  onToggleTransfer,
}: ImportTransactionRowProps) {
  const formattedDate = new Date(transaction.date).toLocaleDateString();
  const formattedAmount = transaction.amount.toLocaleString("en-US", {
    style: "currency",
    currency: "USD",
  });

  // Filter out the current account from the transfer dropdown
  const availableAccounts = accounts.filter(a => a.name !== currentAccountName);

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
        <label>{transaction.isTransfer ? "Transfer to/from:" : "Category:"}</label>
        {transaction.isTransfer ? (
          <div className="transfer-select-container">
            <select
              value={transaction.transferAccountName || ""}
              onChange={(e) => onTransferAccountChange(index, e.target.value || null)}
              className="category-select transfer-account-select"
            >
              <option value="">Select account...</option>
              {availableAccounts.map((account) => (
                <option key={account.name} value={account.name}>
                  {account.name}
                </option>
              ))}
            </select>
            <button
              type="button"
              className="btn-unmark-transfer"
              onClick={() => onToggleTransfer(index)}
              title="Not a transfer"
            >
              ✕
            </button>
          </div>
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
