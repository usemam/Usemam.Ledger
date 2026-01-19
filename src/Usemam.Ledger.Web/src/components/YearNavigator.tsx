interface YearNavigatorProps {
  year: number;
  onYearChange: (year: number) => void;
}

export function YearNavigator({ year, onYearChange }: YearNavigatorProps) {
  const currentYear = new Date().getFullYear();

  return (
    <div className="year-navigator">
      <button
        className="year-nav-button"
        onClick={() => onYearChange(year - 1)}
        aria-label="Previous year"
      >
        &larr;
      </button>
      <span className="year-display">{year}</span>
      <button
        className="year-nav-button"
        onClick={() => onYearChange(year + 1)}
        disabled={year >= currentYear}
        aria-label="Next year"
      >
        &rarr;
      </button>
    </div>
  );
}
