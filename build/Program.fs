open System
open System.Text
open System.IO
open Fake.IO
open Fake.Core
open System.Xml

let path xs = Path.Combine(Array.ofList xs)

let solutionRoot = Files.findParent __SOURCE_DIRECTORY__ "README.md";

let src = path [ solutionRoot; "src" ]

let dotnet args dir msg =
    if Shell.Exec(Tools.dotnet, args, dir) <> 0
    then failwith msg

let python args dir msg =
    if Shell.Exec("python", args, dir) <> 0
    then failwith msg

/// <summary>
/// Changes the project in src from Exe to Library
/// and removes the included Program.fs entry point. 
/// This is applied to the project before publishing.
/// </summary>
let turnIntoLibrary() =     
    let projectFile = path [ src; "Fable.SimpleJson.Python.fsproj" ]
    let content = File.ReadAllText projectFile
    let project = new XmlDocument()
    project.LoadXml content
    let outputType = project.SelectSingleNode "descendant::OutputType"
    outputType.InnerText <- "Library"
    let program = project.SelectSingleNode "descendant::Compile[@Include='Program.fs']"
    program.ParentNode.RemoveChild(program) |> ignore
    project.Save(projectFile)

/// <summary>
/// Convert back to exe after publishing as  a library
/// </summary>
let turnBackIntoExe() =     
    let projectFile = path [ src; "Fable.SimpleJson.Python.fsproj" ]
    let content = File.ReadAllText projectFile
    let project = new XmlDocument()
    project.LoadXml content
    let outputType = project.SelectSingleNode "descendant::OutputType"
    outputType.InnerText <- "Exe"
    let compileElements = project.SelectNodes "descendant::Compile"
    let compileParent = compileElements.[0].ParentNode
    let program = project.CreateElement("Compile")
    program.SetAttribute("Include", "Program.fs")
    compileParent.AppendChild(program) |> ignore
    project.Save(projectFile)

let publish() =
    Shell.deleteDir (path [ src; "bin" ])
    Shell.deleteDir (path [ src; "obj" ])

    dotnet "pack -c Release" src "Packing the library failed"

    let nugetKey =
        match Environment.environVarOrNone "NUGET_KEY" with
        | Some nugetKey -> nugetKey
        | None -> failwith "The Nuget API key must be set in a NUGET_KEY environmental variable"

    let nugetPath =
        Directory.GetFiles(path [ src; "bin"; "Release" ])
        |> Seq.head
        |> Path.GetFullPath

    dotnet (sprintf "nuget push %s -s nuget.org -k %s" nugetPath nugetKey) src "Pushing the library to nuget failed"

[<EntryPoint>]
let main argv =
    match argv with
    | [| "publish" |] -> 
        turnIntoLibrary()
        publish()
        turnBackIntoExe()
    
    // for testing
    | [| "turn-into-library" |] -> turnIntoLibrary()
    | [| "turn-into-exe" |] -> turnBackIntoExe()
    | _ -> ()
    0