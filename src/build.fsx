#r "packages/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Testing

let outDir = "./../out"
let testDir = outDir @@ "test"
let buildDir = outDir @@ "build"
let releaseDir = outDir @@ "release"

Target "Clean" (fun _ ->
    !! "**/bin" ++ "**/obj" |> CleanDirs
    "./../out" |> DeleteDir
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

Target "Release" (fun _ ->
    !! "EvoDistroLisa.CLI\EvoDistroLisa.CLI.fsproj"
    |> MSBuildRelease (releaseDir @@ "cli") "Release"
    |> Log "Release-Output: "
)

RunTargetOrDefault "Build"