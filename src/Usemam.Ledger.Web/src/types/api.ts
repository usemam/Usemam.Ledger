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
