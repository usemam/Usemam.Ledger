module Usemam.Ledger.Backup.Zip

open System
open System.IO
open ICSharpCode.SharpZipLib.Zip

let private addZipEntry (stream : ZipOutputStream) (buffer : byte[]) (item : string) =
    let info = new FileInfo(item)
    let entry = new ZipEntry(info.Name |> ZipEntry.CleanName)
    entry.DateTime <- info.LastWriteTime
    entry.Size <- info.Length
    use fileStream = info.OpenRead()
    stream.PutNextEntry(entry)
    let length = ref fileStream.Length
    fileStream.Seek(0L, SeekOrigin.Begin) |> ignore
    while !length > 0L do
        let count = fileStream.Read(buffer, 0, buffer.Length)
        stream.Write(buffer, 0, count)
        length := !length - (int64 count)

let create fileName files =
    use stream = new ZipOutputStream(File.Create(fileName))
    stream.SetLevel 9
    let buffer = Array.create 32768 0uy
    for item in files do
        addZipEntry stream buffer item
    stream.Finish()