import { Routes, Route } from "react-router-dom";
import { AccountList } from "./components/AccountList";
import { AccountDetails } from "./components/AccountDetails";
import "./App.css";

function App() {
  return (
    <div className="app">
      <header>
        <h1>Ledger</h1>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<AccountList />} />
          <Route path="/accounts/:name" element={<AccountDetails />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
