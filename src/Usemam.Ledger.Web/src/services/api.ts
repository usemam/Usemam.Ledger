import type { AccountDto, SpendingReportDto, TransactionDto } from "../types/api";

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
