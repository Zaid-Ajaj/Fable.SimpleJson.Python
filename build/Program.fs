open System
open System.Text
open System.IO
open Fake.IO
open Fake.Core
open System.Xml

module List = 
    let exec xs = List.iter (fun task -> task()) xs


let path xs = Path.Combine(Array.ofList xs)

let solutionRoot = Files.findParent __SOURCE_DIRECTORY__ "README.md";

let src = path [ solutionRoot; "src" ]
let tests = path [ solutionRoot; "test"  ]

let dotnet args dir msg =
    if Shell.Exec(Tools.dotnet, args, dir) <> 0
    then failwith msg

let python args dir msg =
    if Shell.Exec("python", args, dir) <> 0
    then failwith msg

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

let cleanDirectories() = Shell.deleteDirs [
    path [ src; "obj" ]
    path [ src; "bin" ]
    path [ src; "fable_modules" ]
    path [ src; "__pycache__" ]
    path [ tests; "src" ]
    path [ tests; "fable_modules" ]
    path [ tests; "__pycache__" ]
    path [ tests; "bin" ]
    path [ tests; "obj" ]
]

let cleanPythonFiles() = 
    let srcPythonFiles = System.IO.Directory.GetFiles(src, "*.py")
    let testPythonFiles = System.IO.Directory.GetFiles(tests, "*.py")
    Array.iter Shell.rm srcPythonFiles
    Array.iter Shell.rm testPythonFiles

let clean() = List.exec [cleanDirectories; cleanPythonFiles]

let test() = 
    dotnet "fable-py" tests "Compiling the tests project to Python failed"
    python "program.py" tests "Running the tests failed"

[<EntryPoint>]
let main argv =
    match argv with
    | [| "publish" |] -> List.exec [clean; test; publish]
    | [| "clean" |] -> clean()
    | [| "test" |] -> List.exec [clean; test]
    | _ -> ()
    0