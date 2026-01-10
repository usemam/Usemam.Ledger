# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Personal finance ledger application written primarily in F# with a C# component for console input handling. Tracks accounts, transactions (credits, debits, transfers).

## Build & Test Commands

```bash
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/Usemam.Ledger.Domain.Tests/Usemam.Ledger.Domain.Tests.fsproj

# Run the console application
dotnet run --project src/Usemam.Ledger.Console/Usemam.Ledger.Console.fsproj
```

## Architecture

### Solution Structure

- **Usemam.Ledger.Domain** (F#, .NET Standard 2.0) - Core domain models and business logic
- **Usemam.Ledger.Console** (F#, .NET 9.0) - Main executable with CLI parser and services
- **Usemam.Ledger.CommandLine** (C#, .NET 9.0) - Rich console input with history and autocomplete
- **Usemam.Ledger.Learning** (F#, .NET Standard 2.0) - Naive Bayes transaction classifier

### Key Patterns

**Command-Query Separation:** Commands implement `ICommand` with `run` and `rollback` methods for state modification and undo support. Queries implement `IQuery<'T>` for read-only operations.

**Result Type:** Custom F# discriminated union (`Result<'T>` with `Ok`/`Error` cases) used throughout for error handling instead of exceptions.

**State Management:** Immutable `State` type contains `IAccounts` and `ITransactions`. Commands return new state; `CommandTracker` maintains undo/redo stacks.

**Parser Combinators:** FParsec library used in `Parser.fs` for natural language command parsing (e.g., "transfer 100 from checking to savings").

### Domain Types

- `Money` = Amount + Currency (USD only)
- `Account` = Name, Balance, CreditLimit, Created, IsClosed
- `Transaction` = Date, Sum, Description (variants: Transfer, Credit, Debit)

### Data Persistence

JSON files (`accounts.db`, `transactions.db`) serialized with Newtonsoft.Json. Loaded at startup, saved on exit.

## Testing

Uses xUnit with FsCheck for property-based testing. Tests use custom `Arb` generators for domain types (see `Generators.fs`).

## Configuration

`appsettings.json` contains:
- File paths for data storage
