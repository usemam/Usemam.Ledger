import { useMutation, useQueryClient } from "@tanstack/react-query";
import { parseStatement, confirmImport } from "../services/api";
import type { ImportConfirmDto } from "../types/api";

export function useParseStatement() {
  return useMutation({
    mutationFn: ({
      file,
      accountName,
      format,
    }: {
      file: File;
      accountName: string;
      format?: string;
    }) => parseStatement(file, accountName, format),
  });
}

export function useConfirmImport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: ImportConfirmDto) => confirmImport(request),
    onSuccess: (_data, variables) => {
      // Invalidate transactions query for the account to refresh data
      queryClient.invalidateQueries({
        queryKey: ["accounts", variables.accountName, "transactions"],
      });
    },
  });
}
