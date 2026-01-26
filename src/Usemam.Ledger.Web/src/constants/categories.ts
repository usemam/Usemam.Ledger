export const CATEGORIES = [
  "Misc",
  "Rent",
  "Grocery",
  "Salary",
  "Kid",
  "Car",
  "Gas",
  "Lunch",
  "Utilities",
  "Entertainment",
  "Health",
  "Apparel",
  "Home",
  "Interest",
  "Fees & Taxes",
  "Pet",
  "Tickets",
  "Education",
] as const;

export type Category = (typeof CATEGORIES)[number];
