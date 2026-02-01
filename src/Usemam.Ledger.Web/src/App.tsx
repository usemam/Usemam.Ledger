import { Routes, Route } from "react-router-dom";
import { MainPage } from "./components/MainPage";
import { AccountDetails } from "./components/AccountDetails";
import { ImportPage } from "./components/ImportPage";
import "./App.css";

function App() {
  return (
    <div className="app">
      <header>
        <h1>Ledger</h1>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<MainPage />} />
          <Route path="/accounts/:name" element={<AccountDetails />} />
          <Route path="/accounts/:name/import" element={<ImportPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default App;
