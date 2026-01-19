interface SpendingCellProps {
  value: number;
  isNetTotal?: boolean;
}

export function SpendingCell({ value, isNetTotal = false }: SpendingCellProps) {
  const getCellClass = () => {
    if (value === 0) return "spending-cell spending-cell-zero";
    if (isNetTotal) {
      return value > 0
        ? "spending-cell spending-cell-positive"
        : "spending-cell spending-cell-negative";
    }
    return value > 0
      ? "spending-cell spending-cell-positive"
      : "spending-cell spending-cell-negative";
  };

  const formatValue = (v: number) => {
    return v.toLocaleString("en-US", {
      style: "currency",
      currency: "USD",
      minimumFractionDigits: 2,
    });
  };

  return <td className={getCellClass()}>{formatValue(value)}</td>;
}
