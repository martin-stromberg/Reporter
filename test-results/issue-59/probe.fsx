// Licensed under the PolyForm Noncommercial License 1.0.0 - see the LICENSE file in the project root for details.
#r "D:/Repositories/softwareschmiede/efbace56-7370-47f4-b106-10b83d0e137c/src/Reporter.Core/bin/Release/net10.0/Reporter.Core.dll"

open System
open Reporter.Core.Services

let query = if fsi.CommandLineArgs.Length > 1 then fsi.CommandLineArgs.[1] else "http://127.0.0.1:8099/"
let svc = FeedSearchService(new Net.Http.HttpClient())
let sw = Diagnostics.Stopwatch.StartNew()

try
    let r = svc.SearchAsync(query).Result
    sw.Stop()
    printfn "OK %d results in %dms" r.Count sw.ElapsedMilliseconds
    r |> Seq.iter (fun x -> printfn "  %s %A score=%f" x.FeedUrl x.MatchKind x.Score)
with
| e ->
    sw.Stop()
    printfn "FAILED after %dms" sw.ElapsedMilliseconds
    printfn "%s" (e.ToString())
