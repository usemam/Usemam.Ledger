import { useQuery } from "@tanstack/react-query";
import { getTransactions, getTransactionsForAccount } from "../services/api";

export function useTransactions(startDate?: string, endDate?: string) {
  return useQuery({
    queryKey: ["transactions", startDate, endDate],
    queryFn: () => getTransactions(startDate, endDate),
  });
}

export function useAccountTransactions(accountName: string) {
  return useQuery({
    queryKey: ["transactions", "account", accountName],
    queryFn: () => getTransactionsForAccount(accountName),
    enabled: !!accountName,
  });
}
