export interface MoneyDto {
  amount: number;
  currency: string;
}

export interface AccountDto {
  name: string;
  isClosed: boolean;
  created: string;
  balance: MoneyDto;
  creditLimit: MoneyDto;
}

export interface TransactionDto {
  date: string;
  amount: MoneyDto;
  type: "Transfer" | "Credit" | "Debit";
  sourceAccount: string | null;
  destinationAccount: string | null;
  creditSource: string | null;
  debitTarget: string | null;
  description: string | null;
}

export interface CategorySpendingDto {
  category: string;
  monthlyAmounts: number[];
  yearTotal: number;
}

export interface SpendingReportDto {
  year: number;
  categories: CategorySpendingDto[];
  monthlyTotals: number[];
  yearlyNet: number;
}

// Import types

export interface ParsedTransactionDto {
  date: string;
  amount: number;
  description: string;
  category: string;
  isCredit: boolean;
  isDuplicate: boolean;
}

export interface ImportSummaryDto {
  total: number;
  credits: number;
  debits: number;
  duplicates: number;
}

export interface ParseResultDto {
  accountName: string;
  detectedFormat: string;
  transactions: ParsedTransactionDto[];
  summary: ImportSummaryDto;
}

export interface ImportTransactionDto {
  date: string;
  amount: number;
  description: string;
  category: string;
  isCredit: boolean;
}

export interface ImportConfirmDto {
  accountName: string;
  transactions: ImportTransactionDto[];
}

export interface ImportResultDto {
  success: boolean;
  imported: number;
  message: string;
}
