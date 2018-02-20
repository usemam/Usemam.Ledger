module Usemam.Ledger.Backup.BackupProperties

open System
open Usemam.Ledger.Domain
open Usemam.Ledger.Backup
open FsCheck
open FsCheck.Xunit

type MockStorage (files: string list) =
    interface IRemoteStorage with
        member this.uploadFile _ = Success ()
        member this.listFiles () = Success files

type IntArbitrary =
    static member Int32() = Gen.choose(10, 12) |> Arb.fromGen

[<Property(Arbitrary = [| typeof<IntArbitrary> |])>]
let ``backup should be skipped if already run today``
    (day : int)
    (month : int)
    (year : int) =
    let fullYear = (2000 + year)
    let clock = fun () -> DateTimeOffset.Parse(sprintf "%i/%i/%i" month day fullYear)
    let storage = MockStorage([sprintf "ledger_bak_%i%i%i.zip" month day fullYear])
    let backupResult = BackupFacade.run [] storage clock
    match backupResult with
    | Failure m -> m = "Backup skipped"
    | _ -> false