import { useQuery } from "@tanstack/react-query";
import { getSpendingReport } from "../services/api";

export function useSpendingReport(year: number) {
  return useQuery({
    queryKey: ["spending-report", year],
    queryFn: () => getSpendingReport(year),
  });
}
