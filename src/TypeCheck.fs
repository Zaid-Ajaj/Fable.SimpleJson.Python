namespace Fable.SimpleJson.Python

open Fable.Core
open Fable.Core.JsInterop

module TypeCheck =

    [<Emit("isinstance($0, str)")>]
    let typeofString (x: obj) : bool = nativeOnly

    [<Emit("isinstance($0, bool)")>]
    let typeofBool (x: obj) : bool = nativeOnly

    [<Emit("isinstance($0, int) OR isinstance($0, float)")>]
    let typeofNumber (x: obj) : bool = nativeOnly

    [<Emit("isinstance($0, dict)")>]
    let typeofObject (x: obj) : bool = nativeOnly

    [<Emit("isinstance($0, list)")>]
    let typeofList (x: obj) : bool = nativeOnly

    let (|NativeString|_|) (x: obj) =
        if typeofString x
        then Some (unbox<string> x)
        else None

    let (|NativeBool|_|) (x: obj) =
        if typeofBool x
        then Some (unbox<bool> x)
        else None

    let (|NativeNumber|_|) (x: obj) =
        if typeofNumber x
        then Some (unbox<float> x)
        else None

    let (|NativeObject|_|) (x: obj) =
        if typeofObject x
        then Some x
        else None

    let (|Null|_|) (x: obj) =
        if isNull x
        then Some x
        else None

    let (|NativeArray|_|) (x: obj) =
        if typeofList x
        then Some (unbox<obj[]> x)
        else None