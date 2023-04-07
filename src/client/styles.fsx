#r "nuget: JavaScriptEngineSwitcher.V8.Native.win-x64"
#r "nuget: JavaScriptEngineSwitcher.V8.Native.linux-x64"
#r "nuget: JavaScriptEngineSwitcher.V8.Native.osx-x64"
#r "nuget: JavaScriptEngineSwitcher.V8"
#r "nuget: DartSassHost"
#r "nuget: FSharp.Control.Reactive, 5.*"

open System
open System.IO
open DartSassHost
open DartSassHost.Helpers
open JavaScriptEngineSwitcher.V8
open FSharp.Control.Reactive

let sassCompiler = new SassCompiler(new V8JsEngineFactory())

let (</>) a b = Path.Combine(a, b)

let bootstrap = __SOURCE_DIRECTORY__ </> "bootstrap"

let styles = __SOURCE_DIRECTORY__ </> "styles"

let entrypoint = __SOURCE_DIRECTORY__ </> "styles" </> "style.sass"

let output = __SOURCE_DIRECTORY__ </> "src" </> "styles" </> "style.css"

let compileSass () =
    try

        let result =
            sassCompiler.CompileFile(
                entrypoint,
                outputPath = output,
                options = CompilationOptions(IncludePaths = [| bootstrap; styles |])
            )

        File.WriteAllText(output, result.CompiledContent)
        printfn "Compiled %s at %A" output DateTime.Now

    with
    | :? SassCompilerLoadException as sclex ->
        printfn
            "During loading of Sass compiler an error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails sclex)
    | :? SassCompilationException as sce ->
        printfn
            "During compilation of SCSS code an error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails sce)
    | :? SassException as e ->
        printfn
            "During working of Sass compiler an unknown error occurred. See details:\n%s"
            (SassErrorHelpers.GenerateErrorDetails e)
    | ex -> printfn "Unexpected exception during Sass compilation: %A" ex

let isWatch =
    match Seq.tryLast fsi.CommandLineArgs with
    | Some "--watch" -> true
    | _ -> false

if isWatch then
    let fsw = new FileSystemWatcher(styles)
    fsw.IncludeSubdirectories <- true
    fsw.Filters.Add "*.sass"
    fsw.NotifyFilter <- NotifyFilters.FileName ||| NotifyFilters.Size
    fsw.EnableRaisingEvents <- true

    let mapEvent ev = Observable.map (fun _ -> ()) ev

    let subscriber =
        [| mapEvent fsw.Renamed
           mapEvent fsw.Changed
           mapEvent fsw.Deleted
           mapEvent fsw.Created |]
        |> Observable.mergeArray
        |> Observable.startWith [| () |]
        |> Observable.throttle (TimeSpan.FromMilliseconds 200.)
        |> Observable.subscribe compileSass

    let _ = Console.ReadLine()
    subscriber.Dispose()

    printfn "Goodbye"
    exit 0
else
    compileSass ()
    exit 0
