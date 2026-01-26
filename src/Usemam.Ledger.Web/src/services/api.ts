import type {
  AccountDto,
  SpendingReportDto,
  TransactionDto,
  ParseResultDto,
  ImportConfirmDto,
  ImportResultDto,
} from "../types/api";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5000";

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`HTTP error! status: ${response.status}`);
  }
  return response.json();
}

export async function getAccounts(): Promise<AccountDto[]> {
  return fetchJson<AccountDto[]>(`${API_BASE_URL}/api/accounts`);
}

export async function getAccountByName(name: string): Promise<AccountDto> {
  return fetchJson<AccountDto>(
    `${API_BASE_URL}/api/accounts/${encodeURIComponent(name)}`
  );
}

export async function getTransactionsForAccount(
  name: string
): Promise<TransactionDto[]> {
  return fetchJson<TransactionDto[]>(
    `${API_BASE_URL}/api/accounts/${encodeURIComponent(name)}/transactions`
  );
}

export async function getSpendingReport(year: number): Promise<SpendingReportDto> {
  return fetchJson<SpendingReportDto>(
    `${API_BASE_URL}/api/reports/spending/${year}`
  );
}

// Import functions

export async function parseStatement(
  file: File,
  accountName: string,
  format?: string
): Promise<ParseResultDto> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("accountName", accountName);
  if (format) {
    formData.append("format", format);
  }

  const response = await fetch(`${API_BASE_URL}/api/import/parse`, {
    method: "POST",
    body: formData,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || `HTTP error! status: ${response.status}`);
  }

  return response.json();
}

export async function confirmImport(
  request: ImportConfirmDto
): Promise<ImportResultDto> {
  const response = await fetch(`${API_BASE_URL}/api/import/confirm`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || `HTTP error! status: ${response.status}`);
  }

  return response.json();
}
