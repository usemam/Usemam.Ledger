module Usemam.Ledger.API.Program

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

open Giraffe

open Usemam.Ledger.API.StateService
open Usemam.Ledger.API.Routes

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Add configuration
    builder.Configuration
        .SetBasePath(builder.Environment.ContentRootPath)
        .AddJsonFile("appsettings.json", optional = false, reloadOnChange = true)
        |> ignore

    // Get CORS origins from configuration
    let corsOrigins =
        builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
        |> Option.ofObj
        |> Option.defaultValue [| "http://localhost:5173"; "http://localhost:3000" |]

    // Add services
    builder.Services.AddCors(fun options ->
        options.AddPolicy("AllowReactApp", fun policy ->
            policy
                .WithOrigins(corsOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
            |> ignore
        )
    ) |> ignore

    builder.Services.AddGiraffe() |> ignore
    builder.Services.AddSingleton<IStateService>(fun sp ->
        StateService(sp.GetRequiredService<IConfiguration>()) :> IStateService
    ) |> ignore

    let app = builder.Build()

    app.UseCors("AllowReactApp") |> ignore
    app.UseGiraffe(webApp)

    app.Run()

    0
