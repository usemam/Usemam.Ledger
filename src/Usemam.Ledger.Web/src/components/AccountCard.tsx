import { Link } from "react-router-dom";
import type { AccountDto } from "../types/api";
import { Money } from "./Money";

interface AccountCardProps {
  account: AccountDto;
}

export function AccountCard({ account }: AccountCardProps) {
  return (
    <div className="account-card">
      <Link to={`/accounts/${encodeURIComponent(account.name)}`}>
        <h3>{account.name}</h3>
        <div className="account-balance">
          <Money money={account.balance} />
        </div>
        {account.creditLimit.amount > 0 && (
          <div className="account-credit">
            Credit: <Money money={account.creditLimit} />
          </div>
        )}
        <div className="account-created">
          Created: {new Date(account.created).toLocaleDateString()}
        </div>
      </Link>
    </div>
  );
}
