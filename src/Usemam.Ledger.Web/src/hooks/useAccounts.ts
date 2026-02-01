import { useQuery, useInfiniteQuery } from "@tanstack/react-query";
import { getAccounts, getAccountByName, getTransactionsForAccount } from "../services/api";

const PAGE_SIZE = 50;

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
  return useInfiniteQuery({
    queryKey: ["accounts", name, "transactions"],
    queryFn: ({ pageParam = 0 }) => getTransactionsForAccount(name, pageParam, PAGE_SIZE),
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      if (!lastPage.hasMore) return undefined;
      return allPages.length * PAGE_SIZE;
    },
    enabled: !!name,
  });
}
