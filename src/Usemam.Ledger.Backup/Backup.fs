﻿namespace Usemam.Ledger.Backup

open System
open System.IO

open Usemam.Ledger.Domain
open Usemam.Ledger.Domain.Result

module BackupFacade =

    let private backupFilePrefix = "ledger_bak_"

    let private createBackupFileName (clock : unit -> DateTimeOffset) =
        let now = clock()
        let dd, mm, yyyy = now.Day, now.Month, now.Year
        sprintf "%s%02i%02i%i.zip" backupFilePrefix mm dd yyyy

    type private BackupSchedule(storage : IRemoteStorage) =
        member this.isBackupNeeded (clock : unit -> DateTimeOffset) =
            result {
                let now = clock()
                let dd, mm, yyyy = now.Day, now.Month, now.Year
                let! files = storage.listFiles()
                return
                    files
                    |> Seq.filter (fun f -> f.StartsWith backupFilePrefix)
                    |> Seq.map (fun f -> f.Substring(11, 8))
                    |> Seq.map (fun d ->
                        Int32.Parse(d.Substring(0, 2)), Int32.Parse(d.Substring(2, 2)), Int32.Parse(d.Substring(4, 4)))
                    |> Seq.exists (fun (m, d, y) -> d = dd && m = mm && y = yyyy)
                    |> not
            }

    let run files storage clock =
        result {
            let schedule = BackupSchedule(storage)
            let! scheduleCheck = schedule.isBackupNeeded clock
            match scheduleCheck with
            | false -> return! Failure "Backup skipped"
            | true ->
                let workingDir = Environment.CurrentDirectory
                let backupFilePath = Path.Combine(workingDir, createBackupFileName clock)
                let! _ = tryCatch (Zip.create backupFilePath) files
                return! storage.uploadFile backupFilePath 
        }