namespace EvoDistroLisa.Engine.Tests

open Xunit
open EvoDistroLisa.Engine

module ArrayTests = 

    [<Fact>]
    let moveItemsInArrayForward () =
        let actual = [|0; 1; 2; 3; 4; 5|] |> Array.move 1 4 
        let expected = [|0; 2; 3; 1; 4; 5|]
        Assert.True((expected = actual))

    [<Fact>]
    let moveItemsInArrayBackward () =
        let actual = [|0; 1; 2; 3; 4; 5|] |> Array.move 4 1
        let expected = [|0; 4; 1; 2; 3; 5|]
        Assert.True((expected = actual))




