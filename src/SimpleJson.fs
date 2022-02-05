namespace Fable.SimpleJson.Python

open System

open Fable.Core
open Fable.Import
open Fable.Python.Json

module InteropUtil =
    [<Emit("$1[$0]")>]
    let get<'a> (key: string) (x: obj) : 'a = nativeOnly
    let isDateOffset (value: obj) =
        match value with
        | :? DateTimeOffset -> true
        | _ -> false

    [<Emit("print($0)")>]
    let log (x: 'a) : unit = nativeOnly

    [<Emit "{}">]
    let createEmptyObject() : obj = nativeOnly

module SimpleJson =
    /// Stringifies a Json object back to string representation
    let rec toString = function
        | JNull -> "null"
        | JBool true -> "true"
        | JBool false -> "false"
        | JNumber number -> string number
        | JString text -> sprintf "\"%s\"" text
        | JArray elements ->
            elements
            |> List.map toString
            |> String.concat ","
            |> sprintf "[%s]"
        | JObject map ->
            map
            |> Map.toList
            |> List.map (fun (key,value) -> sprintf "\"%s\":%s" key (toString value))
            |> String.concat ","
            |> sprintf "{%s}"

    [<Emit "$0.keys()">]
    let objectKeys (x: obj) : string seq = nativeOnly
    let rec internal parseNative' (x: obj) =
        match x with
        | TypeCheck.NativeString str -> JString str
        | TypeCheck.NativeNumber number -> JNumber number
        | TypeCheck.NativeBool value -> JBool value
        | TypeCheck.Null _ -> JNull
        | TypeCheck.NativeArray arr -> JArray (List.ofArray (Array.map parseNative' arr))
        | TypeCheck.NativeObject object ->
            [ for key in objectKeys object -> key, parseNative' (InteropUtil.get<obj> key object)  ]
            |> Map.ofList
            |> JObject
        | _ -> JNull

    /// Parses and converts the input string to Json using Javascript's native parsing capabilities
    let parseNative (input: string) =
        let parsed = json.loads input
        parseNative' parsed

    let tryParseNative (input: string) =
        try Some (parseNative input)
        with | ex -> None

    /// Tries to convert an object literal to the Json by calling JSON.stringify on the object first
    let fromObjectLiteral (x: 'a) =
        try Some (parseNative' x)
        with | _ -> None

    /// Transforms all keys of the objects within the Json structure
    let rec mapKeys f = function
        | JObject dictionary ->
            dictionary
            |> Map.toList
            |> List.map (fun (key, value) -> f key, mapKeys f value)
            |> Map.ofList
            |> JObject
        | JArray values ->
            values
            |> List.map (mapKeys f)
            |> JArray
        | otherJsonValue -> otherJsonValue

    [<Emit "$2[$0] = $1">]
    let private setValue (key: string) (value: obj) (destination: obj) = jsNative
    let rec toPlainObject (input: Json) : obj =
        match input with
        | JNull -> unbox null
        | JBool value -> unbox value
        | JNumber value -> unbox value
        | JString value -> unbox value
        | JArray values ->
            let array = new ResizeArray<obj>()
            for value in values do array.Add(toPlainObject value)
            unbox array
        | JObject map ->
            let jsObject = InteropUtil.createEmptyObject()
            for (key, value) in Map.toList map do
                setValue key (toPlainObject value) jsObject
            unbox jsObject

    /// Transforms object values recursively using function `f` that takes the key and value of the object and returns a new value
    let rec mapbyKey f = function
        | JObject dictionary ->
            dictionary
            |> Map.toList
            |> List.map (fun (key, value) -> key, f key value)
            |> Map.ofList
            |> JObject
        | JArray values ->
            values
            |> List.map (mapbyKey f)
            |> JArray
        | otherJsonValue -> otherJsonValue

    /// Transforms keys of object selectively by path segments
    let mapKeysByPath f json =
        let rec mapKey xs = function
            | JObject dictionary ->
                dictionary
                |> Map.toList
                |> List.map (fun (key, value) ->
                    let keyPath = List.concat [xs; [key]]
                    match f keyPath with
                    | Some nextKey -> nextKey, mapKey keyPath value
                    | None -> key, mapKey keyPath value)
                |> Map.ofList
                |> JObject
            | JArray values ->
                values
                |> List.map (mapKey xs)
                |> JArray
            | otherJsonValue -> otherJsonValue
        mapKey [] json

    let rec readPath (keys: string list) (input: Json) =
        match keys, input with
        | [ ], _ -> None
        | [ key ], JObject dict -> Map.tryFind key dict
        | firstKey :: rest, JObject dict ->
            match Map.tryFind firstKey dict with
            | Some (JObject nextDict) -> readPath rest (JObject nextDict)
            | _ -> None
        | _ -> None
