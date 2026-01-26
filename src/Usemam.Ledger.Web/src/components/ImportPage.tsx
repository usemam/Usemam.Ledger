import { useState, useRef } from "react";
import { useParams, useNavigate, Link } from "react-router-dom";
import { useParseStatement, useConfirmImport } from "../hooks/useImport";
import {
  ImportTransactionRow,
  ImportTransactionCard,
} from "./ImportTransactionRow";
import type { ParsedTransactionDto } from "../types/api";

interface ImportTransaction extends ParsedTransactionDto {
  selected: boolean;
}

type ImportState = "initial" | "parsing" | "preview" | "importing";

export function ImportPage() {
  const { name } = useParams<{ name: string }>();
  const decodedName = name ? decodeURIComponent(name) : "";
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [importState, setImportState] = useState<ImportState>("initial");
  const [transactions, setTransactions] = useState<ImportTransaction[]>([]);
  const [detectedFormat, setDetectedFormat] = useState<string>("");
  const [error, setError] = useState<string | null>(null);

  const parseStatement = useParseStatement();
  const confirmImport = useConfirmImport();

  const handleFileSelect = async (
    event: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setError(null);
    setImportState("parsing");

    try {
      const result = await parseStatement.mutateAsync({
        file,
        accountName: decodedName,
      });

      setDetectedFormat(result.detectedFormat);
      setTransactions(
        result.transactions.map((t) => ({
          ...t,
          selected: true,
        }))
      );
      setImportState("preview");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to parse file");
      setImportState("initial");
    }
  };

  const handleToggleSelect = (index: number) => {
    setTransactions((prev) =>
      prev.map((t, i) => (i === index ? { ...t, selected: !t.selected } : t))
    );
  };

  const handleCategoryChange = (index: number, category: string) => {
    setTransactions((prev) =>
      prev.map((t, i) => (i === index ? { ...t, category } : t))
    );
  };

  const handleSelectAll = () => {
    setTransactions((prev) => prev.map((t) => ({ ...t, selected: true })));
  };

  const handleDeselectAll = () => {
    setTransactions((prev) => prev.map((t) => ({ ...t, selected: false })));
  };

  const handleImport = async () => {
    const selectedTransactions = transactions.filter((t) => t.selected);
    if (selectedTransactions.length === 0) return;

    setImportState("importing");
    setError(null);

    try {
      await confirmImport.mutateAsync({
        accountName: decodedName,
        transactions: selectedTransactions.map((t) => ({
          date: t.date,
          amount: t.amount,
          description: t.description,
          category: t.category,
          isCredit: t.isCredit,
        })),
      });

      navigate(`/accounts/${encodeURIComponent(decodedName)}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to import");
      setImportState("preview");
    }
  };

  const handleCancel = () => {
    navigate(`/accounts/${encodeURIComponent(decodedName)}`);
  };

  const handleReset = () => {
    setTransactions([]);
    setDetectedFormat("");
    setError(null);
    setImportState("initial");
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const selectedCount = transactions.filter((t) => t.selected).length;
  const totalCount = transactions.length;

  return (
    <div className="import-page">
      <Link
        to={`/accounts/${encodeURIComponent(decodedName)}`}
        className="back-link"
      >
        &larr; Back to {decodedName}
      </Link>

      <h2>Import Statement to {decodedName}</h2>

      {error && <div className="import-error">{error}</div>}

      {importState === "initial" && (
        <div className="import-upload">
          <p>Select a CSV bank statement file to import transactions.</p>
          <p className="import-formats">
            Supported formats: American Express, Apple Card, Citi, Wells Fargo,
            Discover
          </p>
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv"
            onChange={handleFileSelect}
            className="file-input"
          />
        </div>
      )}

      {importState === "parsing" && (
        <div className="import-loading">
          <p>Parsing statement...</p>
        </div>
      )}

      {importState === "preview" && (
        <>
          <div className="import-summary">
            <p>
              Detected format: <strong>{detectedFormat}</strong>
            </p>
            <p>
              {selectedCount} of {totalCount} transactions selected for import
            </p>
          </div>

          <div className="import-actions-top">
            <button onClick={handleSelectAll} className="btn-secondary">
              Select All
            </button>
            <button onClick={handleDeselectAll} className="btn-secondary">
              Deselect All
            </button>
            <button onClick={handleReset} className="btn-secondary">
              Choose Different File
            </button>
          </div>

          {/* Desktop table view */}
          <div className="import-table-container">
            <table className="import-table">
              <thead>
                <tr>
                  <th className="checkbox-col"></th>
                  <th>Date</th>
                  <th>Amount</th>
                  <th>Description</th>
                  <th>Category</th>
                </tr>
              </thead>
              <tbody>
                {transactions.map((transaction, index) => (
                  <ImportTransactionRow
                    key={index}
                    transaction={transaction}
                    index={index}
                    onToggleSelect={handleToggleSelect}
                    onCategoryChange={handleCategoryChange}
                  />
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile card view */}
          <div className="import-cards-container">
            {transactions.map((transaction, index) => (
              <ImportTransactionCard
                key={index}
                transaction={transaction}
                index={index}
                onToggleSelect={handleToggleSelect}
                onCategoryChange={handleCategoryChange}
              />
            ))}
          </div>

          <div className="import-actions-bottom">
            <button
              onClick={handleImport}
              disabled={selectedCount === 0}
              className="btn-primary"
            >
              Import {selectedCount} Transaction{selectedCount !== 1 ? "s" : ""}
            </button>
            <button onClick={handleCancel} className="btn-secondary">
              Cancel
            </button>
          </div>
        </>
      )}

      {importState === "importing" && (
        <div className="import-loading">
          <p>Importing transactions...</p>
        </div>
      )}
    </div>
  );
}
