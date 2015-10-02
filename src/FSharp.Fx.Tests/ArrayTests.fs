namespace FSharp.Fx.Tests

module Array = 
    open Xunit
    open FSharp.Fx

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

    [<Fact>]
    let manyMoveTest () =
        let input = [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        let test source target expected = 
            let actual = Array.move source target input
            Assert.True((actual = expected))
        test 0 0 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 0 1 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 0 3 [| "1"; "2"; "0"; "3"; "4"; "5"; "6" |]
        test 0 6 [| "1"; "2"; "3"; "4"; "5"; "0"; "6" |]
        test 0 7 [| "1"; "2"; "3"; "4"; "5"; "6"; "0" |]
        test 6 0 [| "6"; "0"; "1"; "2"; "3"; "4"; "5" |]
        test 6 3 [| "0"; "1"; "2"; "6"; "3"; "4"; "5" |]
        test 6 6 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 6 7 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 3 0 [| "3"; "0"; "1"; "2"; "4"; "5"; "6" |]
        test 3 1 [| "0"; "3"; "1"; "2"; "4"; "5"; "6" |]
        test 3 3 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 3 4 [| "0"; "1"; "2"; "3"; "4"; "5"; "6" |]
        test 3 5 [| "0"; "1"; "2"; "4"; "3"; "5"; "6" |]
        test 3 6 [| "0"; "1"; "2"; "4"; "5"; "3"; "6" |]
        test 3 7 [| "0"; "1"; "2"; "4"; "5"; "6"; "3" |]
