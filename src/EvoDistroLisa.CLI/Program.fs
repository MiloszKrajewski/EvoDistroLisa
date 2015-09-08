namespace EvoDistroLisa.CLI

open Nessos.Argu

type Arguments = 
    | Listen of port: int
    | Connect of host: string * port: int
    | Agents of int
    interface IArgParserTemplate with
        member x.Usage = 
            match x with
            | Listen _ -> "the port to listen (as server)"
            | Connect _ -> "host and port to connect to (as client)"
            | Agents _ -> "specify number of agents"
