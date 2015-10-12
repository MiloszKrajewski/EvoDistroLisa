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

Target "Test" (fun _ ->
    !! "*.sln"
    |> MSBuildDebug testDir "Build"
    |> Log "Test-Output: "
    
    !! (testDir @@ "*.Tests.dll")
    |> xUnit2 id
)

Target "Build" (fun _ -> 
    !! "*.sln"
    |> MSBuildRelease null "Build"
    |> Log "Build-Output: "
)

Target "Release" (fun _ ->
    !! "EvoDistroLisa.CLI/bin/Release/*.exe"
    ++ "EvoDistroLisa.CLI/bin/Release/*.exe.config"
    ++ "EvoDistroLisa.CLI/bin/Release/*.dll"
    ++ "EvoDistroLisa.CLI/bin/Release/monalisa.png"
    |> CopyFiles "./../out/cli"

    let libz args = 
        { defaultParams with
            Program = "packages/LibZ.Bootstrap/tools/libz.exe"
            WorkingDirectory = "./../out/cli"
            CommandLine = args }
        |> shellExec

    // -e Suave.dll
    libz "add -l evo.libz -i *.dll -e Suave.dll --move" |> ignore
    libz "instrument -a EvoDistroLisa.CLI.exe --libz-file evo.libz" |> ignore

    !! "./../out/cli/EvoDistroLisa.CLI.exe*"
    |> Seq.iter (fun fn -> fn |> Rename (fn.Replace("EvoDistroLisa.CLI.", "evo.")))
)

Target "Zip" (fun _ ->
    !! "./../out/cli/*.*"
    |> Zip "./../out/cli" "./../out/evo-release.zip"
)

"Clean" ==> "Release"
"Build" ==> "Release"
"Release" ==> "Zip"

RunTargetOrDefault "Build"