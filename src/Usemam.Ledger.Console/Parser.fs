module Usemam.Ledger.Console.Parser

open System

open FParsec

open Usemam.Ledger.Console
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

let private pAccountsCommand = strWs Keywords.Accounts |>> (fun _ -> Show Accounts)
let private pLastTrnsCommand =
  pipe2 (strWs Keywords.Last >>. pint) (strWs Keywords.For >>. pName) (fun count account -> LastN (count, account) |> Show)
let private pTotalCommand =
  let pTotal = strWs Keywords.Total |>> (fun _ -> (Clocks.start(), Clocks.machineClock()))
  let pTotalFiltered = pipe2 (strWs Keywords.Total >>. pDate) (strWs Keywords.To >>. pDate) (fun min max -> (min, max))
  (pTotalFiltered <|> pTotal) |>> (fun (min, max) -> Total (min, max) |> Show)
let private pShowCommand = strWs Keywords.Show >>. (pAccountsCommand <|> pLastTrnsCommand <|> pTotalCommand)

let private pAddAccountCommand =
  strWs Keywords.AddAccount >>. pipe3 pName pMoney (opt (strWs Keywords.Credit >>. pMoney)) (
    fun account amount credit -> AddAccount (account, amount, defaultArg credit Amount.zero))

let private pSetCreditLimitCommand =
  strWs Keywords.SetAccount >>. pipe2 pName (strWs Keywords.Credit >>. pMoney) (fun n a -> SetCreditLimit (n, a))

let private pCloseAccountCommand =
  strWs Keywords.CloseAccount >>. pName |>> CloseAccount

let private pOn =
  opt (strWs Keywords.On >>. pDate)
  |>> (fun d ->
    defaultArg (Option.map Clocks.moment d) Clocks.machineClock
    |> On)
let private pFrom = strWs Keywords.From >>. pName |>> From
let private pTo = strWs Keywords.To >>. pName |>> To

let private pTransferCommand =
  strWs Keywords.Transfer >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Transfer (amount, on, from, t0))
let private pCreditCommand =
  strWs Keywords.Credit >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Credit (amount, on, from, t0))
let private pDebitCommand =
  strWs Keywords.Debit >>. pipe4 pMoney pOn pFrom pTo (fun amount on from t0 -> Command.Debit (amount, on, from, t0))

let private pUndoCommand = strWs Keywords.Undo |>> (fun _ -> Undo)
let private pRedoCommand = strWs Keywords.Redo |>> (fun _ -> Redo)
let private pHelpCommand = strWs Keywords.Help |>> (fun _ -> Help)
let private pExitCommand = strWs Keywords.Exit |>> (fun _ -> Exit)

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

let parse input =
  match run pCommand input with
  | ParserResult.Success (command, _, _) -> Success command
  | ParserResult.Failure (errorMsg, _, _) -> Failure errorMsg