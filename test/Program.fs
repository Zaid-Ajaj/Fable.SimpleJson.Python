module Tests

open System
open Fable.SimpleJson.Python

// Poor man's testing "library"
let testCase (name: string) (f: unit -> unit) =
    Console.WriteLine($"⏳ {name}")
    f()
    Console.WriteLine($"✔  {name}")

type Car = {
    id: int;
    model: string
    properties: Option<string list>
}

type RecordWithIntValues = { values: int list option }

type RecordWithStringValues = { values: string list }

type GenericRecord<'t> = { value: 't }

type Maybe<'t> =
    | Nothing
    | Just of 't

testCase "deserializing list of ints works" <| fun () ->
    let output = Json.parseNativeAs<int list> "[1,2,3]"
    if output <> [1;2;3] then failwith "Could not construct a list"

testCase "deserializing list of ints works from record of ints" <| fun () ->
    let output = Json.parseNativeAs<RecordWithIntValues> "{ \"values\": [1,2,3] }"
    if output.values <> Some [1;2;3] then failwith "Could not construct a list of ints"

testCase "deserializing list of ints works from record of strings" <| fun () ->
    let output = Json.parseNativeAs<RecordWithStringValues> "{ \"values\": [\"first\"] }"
    if output.values <> ["first"] then failwith "Could not construct a list of strings"

testCase "Checking IsSome works with option of list" <| fun () ->
    let values = Some [ "electric" ]
    if values.IsNone then failwith "Should be some"
    if not values.IsSome then failwith "Should be some"

testCase "Checking structural equality works with option of list" <| fun () ->
    let values = Some [ "electric" ]
    if values <> Some [ "electric" ] then failwith "Should be equal"

testCase "Checking IsSome works with record that has option of list" <| fun () ->
    let values = { id = 42; model = "BMW"; properties = Some [ "electric" ] }
    if values.properties.IsNone then failwith "Should be some"
    if not values.properties.IsSome then failwith "Should be some"
    if values.properties <> Some [ "electric" ] then failwith "Should be equal"

testCase "Parsing simple record works" <| fun () ->
    let inputJson = "{ \"id\": 42, \"model\": \"BMW\" }"
    let car = Json.parseNativeAs<Car>(inputJson)
    if car.id <> 42 then failwith "id should be 42"
    if car.model <> "BMW" then failwith "model should be John"
    if car.properties.IsSome then failwith "properties should be none"

testCase "Simple record roundtrip works" <| fun () ->
    let input = { id = 42; model = "BMW"; properties = Some [ "electric" ] }
    let inputJson = Json.serialize input
    let car = Json.parseNativeAs<Car>(inputJson)
    if car.id <> 42 then failwith "id should be 42"
    if car.model <> "BMW" then failwith "model should be BMW"
    if car.properties <> Some [ "electric" ] then failwith "properties should be deserialized"

testCase "Structural equality works with deserialized record" <| fun () ->
    let input = { id = 42; model = "BMW"; properties = Some [] }
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<Car>(inputJson)
    if output <> input then failwith "input and output should be the same"
    if output = { id = 40; model = "BMW"; properties = Some [] } then failwith "should be unequal"

testCase "Maybe<'t> roundtrip works" <| fun () ->
    let input = [ Just 42; Nothing ]
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<Maybe<int> list>(inputJson)
    if output <> input then failwith "input and output should be the same"

testCase "Nested Maybe<'t> roundtrip works" <| fun () ->
    let input = [ Just (Just 42); Nothing ]
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<Maybe<Maybe<int>> list>(inputJson)
    if output <> input then failwith "input and output should be the same"

testCase "Generic record of int64 roundtrip works" <| fun () ->
    let input : GenericRecord<int64> list = [ { value = 42L } ]
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<GenericRecord<int64> list>(inputJson)
    if output <> input then failwith "input and output should be the same"

testCase "Generic record of bigint roundtrip works" <| fun () ->
    let input : GenericRecord<bigint> list = [ { value = 42I } ]
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<GenericRecord<bigint> list>(inputJson)
    if output <> input then failwith "input and output should be the same"

testCase "More complicated shapes roundtrip" <| fun () ->
    let input = [ Just { value = Just { value = Just 15L } } ]
    let inputJson = Json.serialize input
    let output = Json.parseNativeAs<list<Maybe<GenericRecord<Maybe<GenericRecord<Maybe<int64>>>>>>>(inputJson)
    if output <> input then failwith "input and output should be the same"

testCase "int64 equality works" <| fun () ->
    let values = [1L; 2L; 3L]
    let sum = List.sum values
    if sum <> 6L then failwith "Sum of bigints should be 6"

testCase "Deserialing set of ints works" <| fun () ->
    let inputJson = "[1,2,3]"
    let output = Json.parseNativeAs<Set<int>> inputJson
    let expected = set [ 1; 2; 3 ]
    if output <> expected then failwith "Set structural equality should work"
    let sum = Seq.sum output
    if sum <> 6 then failwith "sum of the values in the set should be 6"

testCase "Deserializing base64 strings to byte[]" <| fun () ->
    // Encoding.UTF8.GetBytes "Hello" -> [|72uy; 101uy; 108uy; 108uy; 111uy|]
    // Convert.ToBase64String (Encoding.UTF8.GetBytes "Hello") -> "SGVsbG8="
    let inputJson = "{ \"value\": \"SGVsbG8=\" }"
    let output = Json.parseNativeAs<GenericRecord<byte[]>> inputJson
    if output.value <> [|72uy; 101uy; 108uy; 108uy; 111uy|] then failwith "the byte[] should be correct"

testCase "Deserializing anonymous records" <| fun () ->
    let inputJson = "{ \"content\": [1,2,3] }"
    let deserialized = Json.parseNativeAs<{| content: int list |}> inputJson
    let sum = deserialized.content |> List.sum
    if sum <> 6 then failwith "Sum should be 6"

testCase "bigint equality works" <| fun () ->
    let values = [1I; 2I; 3I]
    let sum = List.sum values
    if sum <> 6I then failwith "Sum of bigints should be 6"

testCase "decimal equality works" <| fun () ->
    let values = [1m; 2m; 3m]
    let sum = List.sum values
    if sum <> 6m then failwith "Sum of decimals should be 6"
