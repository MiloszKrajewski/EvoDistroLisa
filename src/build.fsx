#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing

let testDir = "./../out/test"
let buildDir = "./../out/build"

Target "Clean" (fun _ ->
    !! "**/bin" 
    ++ "**/obj" 
    |> CleanDirs

    [testDir; buildDir] 
    |> CleanDirs
)

Target "Build" (fun _ -> 
    !! "*.sln"
    |> MSBuildRelease buildDir "Build"
    |> Log "Build-Output: "
)

Target "Test" (fun _ ->
    !! "*.sln"
    |> MSBuildDebug testDir "Build"
    |> Log "Test-Output: "
    
    !! (testDir @@ "*.Tests.dll")
    |> xUnit2 id
)

RunTargetOrDefault "Build"