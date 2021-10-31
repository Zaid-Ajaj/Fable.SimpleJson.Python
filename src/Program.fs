module Tests

open System
open Fable.SimpleJson.Python

// Poor man's testing "library"
let testCase (name: string) (f: unit -> unit) =
    f()
    Console.WriteLine($"✅ {name}")

type Car = { id: int; model: string }

testCase "Parsing works" <| fun () ->
    let inputJson = "{ \"id\": 42, \"model\": \"BMW\" }"
    let car = Json.parseNativeAs<Car>(inputJson)
    if car.id <> 42 then failwith "id should be 42"
    if car.model <> "BMW" then failwith "model should be John"
