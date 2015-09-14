namespace FSharp.Fx.Tests

module Array = 
    open Xunit
    open FSharp.Fx

    let slowInsert index item (array: 'a[]) = 
        [|
            for i = 0 to index - 1 do yield array.[i]
            yield item
            for i = index to array.Length - 1 do yield array.[i]
        |]

    [<Fact>]
    let insertTest () = 
        let result = Array.insert 0 "x" [| "1"; "2"; "3" |]
        Assert.True((result = [| "x"; "1";  "2"; "3" |]))

    [<Fact>]
    let manyInsertTest () = 
        let input = [| "1"; "2"; "3" |]
        let test index expected = 
            let actual = Array.insert index "x" input
            Assert.True((actual = expected))
        test 0 [| "x"; "1"; "2"; "3" |]
        test 1 [| "1"; "x"; "2"; "3" |]
        test 2 [| "1"; "2"; "x"; "3" |]
        test 3 [| "1"; "2"; "3"; "x" |]
