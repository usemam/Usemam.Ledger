import { useQuery } from "@tanstack/react-query";
import { getAccounts, getAccountByName, getTransactionsForAccount } from "../services/api";

export function useAccounts() {
  return useQuery({
    queryKey: ["accounts"],
    queryFn: getAccounts,
  });
}

export function useAccount(name: string) {
  return useQuery({
    queryKey: ["accounts", name],
    queryFn: () => getAccountByName(name),
    enabled: !!name,
  });
}

export function useAccountTransactions(name: string) {
  return useQuery({
    queryKey: ["accounts", name, "transactions"],
    queryFn: () => getTransactionsForAccount(name),
    enabled: !!name,
  });
}
