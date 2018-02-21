namespace Usemam.Ledger.Backup

open Dropbox.Api
open Dropbox.Api.Files

open System
open System.IO

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

type IRemoteStorage =
    abstract uploadFile : string -> Result<unit>
    abstract listFiles : unit -> Result<string list>

type DropboxStorage(accessToken : string) =
    let listFilesAsync () =
        async {
            use dbx = new DropboxClient(accessToken)
            let! list = dbx.Files.ListFolderAsync(String.Empty) |> Async.AwaitTask
            return
                list.Entries
                |> Seq.filter (fun e -> e.IsFile)
                |> Seq.map (fun e -> e.Name)
                |> Seq.toList
        }
    let uploadFileAsync f =
        async {
            use dbx = new DropboxClient(accessToken)
            use file = File.OpenRead f
            let name = Path.GetFileName f
            let! _ =
                dbx.Files.UploadAsync("/" + name, WriteMode.Overwrite.Instance, body = file)
                |> Async.AwaitTask
            return ()
        }
    interface IRemoteStorage with
        member this.uploadFile fileName =
            tryCatch (fun f -> f |> uploadFileAsync |> Async.RunSynchronously) fileName
        member this.listFiles () =
            tryCatch (fun u -> u |> listFilesAsync |> Async.RunSynchronously) ()