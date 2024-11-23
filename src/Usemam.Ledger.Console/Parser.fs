module Usemam.Ledger.Console.Parser

open System

open FParsec

open Usemam.Ledger.Console.Input
open Usemam.Ledger.Console.Command
open Usemam.Ledger.Domain

type private UserState = unit
type private Parser<'t> = Parser<'t, UserState>

let private str : string -> Parser<string> = pstring
let private ws = spaces
let private strWs s = str s .>> ws

let private pint : Parser<int32> =
  pint32 .>> ws
let private pMoney : Parser<AmountType> =
  pfloat .>> ws |>> (decimal >> Amount.create)
let private pName : Parser<string> =
  between (str "\"") (str "\"") (many1Satisfy (fun c -> c <> '\\' && c <> '"')) .>> ws
let private pDate : Parser<DateTimeOffset> =
  let sep = str "/"
  pipe5 pint32 sep pint32 sep pint32 (fun m _ d _ y -> DateTimeOffset(DateTime(y, m, d))) .>> ws

let private pAccountsCommand = strWs "accounts" |>> (fun _ -> Show Accounts)
let private pLastTrnsCommand =
  pipe2 (strWs "last" >>. pint) (strWs "for" >>. pName) (fun count account -> LastN (count, account) |> Show)
let private pTotalCommand =
  let pTotal = strWs "total" |>> (fun _ -> (Clocks.start(), Clocks.machineClock()))
  let pTotalFiltered = pipe2 (strWs "total" >>. pDate) (strWs "to" >>. pDate) (fun min max -> (min, max))
  (pTotalFiltered <|> pTotal) |>> (fun (min, max) -> Total (min, max) |> Show)
let private pShowCommand = strWs "show" >>. (pAccountsCommand <|> pLastTrnsCommand <|> pTotalCommand)

let private pAddAccountCommand =
  strWs "add account" >>. pipe3 pName pMoney (opt pMoney) (
    fun account amount credit -> AddAccount (account, amount, defaultArg credit Amount.zero))

let private pSetCreditLimitCommand =
  strWs "set account" >>. pipe2 pName pMoney (fun n a -> SetCreditLimit (n, a))

let private pCloseAccountCommand =
  strWs "close account" >>. pName |>> CloseAccount

let private pOn =
  opt (strWs "on" >>. pDate)
  |>> (fun d ->
    defaultArg (Option.map Clocks.moment d) Clocks.machineClock
    |> On)
let private pFrom = strWs "from" >>. pName |>> From
let private pTo = strWs "to" >>. pName |>> To

let private pTransferCommand =
  strWs "transfer" >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Transfer (amount, on, from, t0))
let private pCreditCommand =
  strWs "credit" >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Credit (amount, on, from, t0))
let private pDebitCommand =
  strWs "debit" >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Debit (amount, on, from, t0))

let private pUndoCommand = strWs "undo" |>> (fun _ -> Undo)
let private pRedoCommand = strWs "redo" |>> (fun _ -> Redo)
let private pHelpCommand = strWs "help" |>> (fun _ -> Help)
let private pExitCommand = strWs "exit" |>> (fun _ -> Exit)

let private pCommand =
  pShowCommand <|>
  pAddAccountCommand <|>
  pSetCreditLimitCommand <|>
  pCloseAccountCommand <|>
  pTransferCommand <|>
  pCreditCommand <|>
  pDebitCommand <|>
  pUndoCommand <|>
  pRedoCommand <|>
  pHelpCommand <|>
  pExitCommand

let private parseKeyPress keyPress =
  match keyPress with
  | KeyPress.ArrowLeft -> Success ArrowLeft
  | KeyPress.ArrowRight -> Success ArrowRight
  | _ -> Failure "This key doesn't have associated command"

let parse input =
  match input with
  | Key keyPress -> parseKeyPress keyPress
  | String inputStr ->
    match run pCommand inputStr with
    | ParserResult.Success (command, _, _) -> Success command
    | ParserResult.Failure (errorMsg, _, _) -> Failure errorMsg