# Fable.SimpleJson.Python [![Nuget](https://img.shields.io/nuget/v/Fable.SimpleJson.Python.svg?colorB=green)](https://www.nuget.org/packages/Fable.SimpleJson.Python)

A library for working with JSON in F# Fable projects targeting Python

### Install from nuget
```
dotnet add package Fable.SimpleJson.Python
```

### Use it in your project
```fs
open Fable.SimpleJson.Python

type Fruit = { name: string }

let fruitJson = Json.serialize { name = "Orange" }

let parsedFruit = Json.parseNativeAs<Fruit> fruitJson
```