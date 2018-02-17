namespace Usemam.Ledger.Backup

open System
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type IRemoteStorage =
    abstract uploadFile : string -> Result<bool>
    abstract listFiles : unit -> Result<string list>

type DropboxStorage() =
    interface IRemoteStorage with
        member this.uploadFile fileName =
            result {
                return true
            }
        member this.listFiles () =
            result {
                return []
            }