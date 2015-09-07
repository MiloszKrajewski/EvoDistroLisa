#r "packages/FAKE/tools/FakeLib.dll"
open Fake

Target "Clean" (fun _ ->
    !! "**/bin"
    ++ "**/obj"
    |> CleanDirs
)

Target "Default" (fun _ -> 
    !! "*.sln"
    |> MSBuildRelease null "Build"
    |> Log "Solution: "
)

"Clean"
    ==> "Default"

RunTargetOrDefault "Default"