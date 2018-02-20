namespace Usemam.Ledger.Backup

open System
open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type IRemoteStorage =
    abstract uploadFile : string -> Result<unit>
    abstract listFiles : unit -> Result<string list>

type DropboxStorage() =
    interface IRemoteStorage with
        member this.uploadFile fileName =
            result {
                return ()
            }
        member this.listFiles () =
            result {
                return []
            }