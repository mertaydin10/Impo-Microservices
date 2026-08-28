#!/usr/bin/env dotnet fsi

// Impo CLI Tool - Advanced Project Management
// Usage: ./impo.fsx <command> [options]

open System
open System.Diagnostics
open System.IO
open System.Net.Http
open System.Text.Json

let mutable args = fsi.CommandLineArgs |> Array.skip 1

type ServiceInfo = {
    Name: string
    Port: int
    Path: string
    Type: string
}

let services = [
    { Name = "Spectator"; Port = 5185; Path = "src/Spectator/Spectator.Api"; Type = "API" }
    { Name = "LocalCrm.Api"; Port = 5057; Path = "src/LocalCrm/LocalCrm.Api"; Type = "API" }
    { Name = "LocalCrm.App"; Port = 5209; Path = "src/LocalCrm/LocalCrm.App"; Type = "Blazor" }
    { Name = "PromptGateway"; Port = 5047; Path = "src/PromptGateway/PromptGateway.Api"; Type = "API" }
]

let red text = sprintf "\x1b[31m%s\x1b[0m" text
let green text = sprintf "\x1b[32m%s\x1b[0m" text
let yellow text = sprintf "\x1b[33m%s\x1b[0m" text
let blue text = sprintf "\x1b[34m%s\x1b[0m" text

let exec cmd args =
    let psi = ProcessStartInfo(cmd, args)
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    let p = Process.Start(psi)
    p.WaitForExit()
    p.ExitCode = 0

let execOutput cmd args =
    let psi = ProcessStartInfo(cmd, args)
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    let p = Process.Start(psi)
    let output = p.StandardOutput.ReadToEnd()
    p.WaitForExit()
    output

let isPortInUse port =
    try
        let output = execOutput "lsof" $"-ti:{port}"
        not (String.IsNullOrWhiteSpace(output))
    with _ -> false

let checkHealth url =
    use client = new HttpClient()
    client.Timeout <- TimeSpan.FromSeconds(2.0)
    try
        let response = client.GetAsync(url).Result
        response.IsSuccessStatusCode
    with _ -> false

let printHeader title =
    printfn ""
    printfn "%s" (blue $"╔═══════════════════════════════════════╗")
    printfn "%s" (blue $"║  {title.PadRight(35)}║")
    printfn "%s" (blue $"╚═══════════════════════════════════════╝")
    printfn ""

let showHelp() =
    printHeader "IMPO CLI - Project Manager"
    printfn "Usage: %s" (yellow "dotnet fsi impo.fsx <command>")
    printfn ""
    printfn "Commands:"
    printfn "  %s           Show service status" (green "status")
    printfn "  %s            Start all services" (green "start")
    printfn "  %s             Stop all services" (green "stop")
    printfn "  %s          Restart services" (green "restart")
    printfn "  %s           Check health" (green "health")
    printfn "  %s             Show logs" (green "logs")
    printfn "  %s            Build project" (green "build")
    printfn "  %s             Run tests" (green "test")
    printfn "  %s             Seed data" (green "seed")
    printfn "  %s             Run demo" (green "demo")
    printfn "  %s             List URLs" (green "urls")
    printfn ""

let showStatus() =
    printHeader "Service Status"
    services |> List.iter (fun svc ->
        let status = if isPortInUse svc.Port then green "RUNNING" else red "STOPPED"
        let url = $"http://localhost:{svc.Port}"
        printfn "  %s  %-20s [%s] %s" 
            (match svc.Name with
             | "Spectator" -> "🏟️"
             | "LocalCrm.Api" -> "👥"
             | "LocalCrm.App" -> "📱"
             | _ -> "🤖")
            svc.Name status url
    )
    printfn ""

let showHealth() =
    printHeader "Health Check"
    services 
    |> List.filter (fun s -> s.Type = "API")
    |> List.iter (fun svc ->
        let url = $"http://localhost:{svc.Port}/health"
        let healthy = checkHealth url
        let status = if healthy then green "✓ OK" else red "✗ DOWN"
        printfn "  %-20s %s" svc.Name status
    )
    printfn ""

let showUrls() =
    printHeader "Service URLs"
    printfn "  %s  Dashboard:         file://%s/dashboard.html" "🎯" Environment.CurrentDirectory
    printfn "  %s  Spectator:         http://localhost:5185" "🏟️"
    printfn "  %s  Spectator Swagger: http://localhost:5185/swagger" "📚"
    printfn "  %s  LocalCrm App:      http://localhost:5209" "📱"
    printfn "  %s  LocalCrm API:      http://localhost:5057" "👥"
    printfn "  %s  LocalCrm Swagger:  http://localhost:5057/swagger" "📚"
    printfn "  %s  PromptGateway:     http://localhost:5047" "🤖"
    printfn "  %s  Gateway Swagger:   http://localhost:5047/swagger" "📚"
    printfn ""

match args with
| [| "help" |] | [||] -> showHelp()
| [| "status" |] -> showStatus()
| [| "health" |] -> showHealth()
| [| "urls" |] -> showUrls()
| [| "build" |] -> 
    printfn "%s" (blue "Building project...")
    if exec "dotnet" "build --nologo" then
        printfn "%s" (green "✓ Build successful!")
    else
        printfn "%s" (red "✗ Build failed!")
| [| "test" |] ->
    printfn "%s" (blue "Running tests...")
    if exec "dotnet" "test --nologo --verbosity quiet" then
        printfn "%s" (green "✓ All tests passed!")
    else
        printfn "%s" (red "✗ Tests failed!")
| [| "start" |] ->
    printfn "%s" (blue "Starting services...")
    exec "bash" "start.sh" |> ignore
| [| "stop" |] ->
    printfn "%s" (yellow "Stopping services...")
    services |> List.iter (fun svc ->
        let pid = execOutput "lsof" $"-ti:{svc.Port}"
        if not (String.IsNullOrWhiteSpace(pid)) then
            exec "kill" pid |> ignore
    )
    printfn "%s" (green "✓ Services stopped!")
| _ -> 
    printfn "%s" (red "Unknown command. Use 'help' for usage.")
