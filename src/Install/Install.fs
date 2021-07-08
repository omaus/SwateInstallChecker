﻿module Install

open System.IO

module Paths = 

    let pRoot() = Directory.GetCurrentDirectory()
    let configPath() = Path.Combine(pRoot (), "config")
    let sideloaderZipPath() = Path.Combine(configPath (), "Set-WebAddin.zip")
    let sideloaderPath() = Path.Combine(configPath (), "Set-WebAddin.exe")
    let manifestPath() = Path.Combine(configPath(), "manifest.xml")
    
    let getUninstallerPath os = 
        let filename = 
            match os with
            | "Windows"         -> "Uninstall.cmd"
            | "Linux" | "MacOS" -> "Uninstall.sh"
            | _                 -> printfn "No suitable Uninstaller existing. OS (%s) not supported" os; ""
        Path.Combine(pRoot(), filename)

    let createConfigFolder () = Directory.CreateDirectory (configPath())

module Download =
    
    [<Literal>]
    let SideloaderUrl = @"https://github.com/davecra/WebAddinSideloader/raw/master/Set-WebAddin%20(v1.0.0.1).zip"

    [<Literal>]
    let ManifestUrl = @"https://raw.githubusercontent.com/nfdi4plants/Swate/developer/.assets/assets/manifest.xml"

    let getOS () = 
        match System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform with
        | _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)  -> "Windows"
        | _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)    -> "Linux"
        | _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)      -> "MacOS"
        | _ when System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD)  -> "FreeBSD (whatever this is)"
        | _                                                                                                                         -> "no supported OS"

    let getUninstallerUrl os =
        match os with
        | "Windows"         -> @"https://raw.githubusercontent.com/omaus/Swate_Install/master/uninstall.cmd"
        | "Linux" | "MacOS" -> @"https://raw.githubusercontent.com/omaus/Swate_Install/master/uninstall.sh"
        | _                 -> printfn "Cannot download Uninstaller. OS (%s) not supported" os; ""

    let webCl = new System.Net.WebClient()

    open Paths

    let downloadSideLoader() = webCl.DownloadFile(SideloaderUrl,sideloaderZipPath())

    let downloadManifestXml() = webCl.DownloadFile(ManifestUrl,manifestPath())

    // testen: Fehlendes Literal attribute könnte problematisch werden
    let downloadUninstaller() = webCl.DownloadFile(getUninstallerUrl (getOS()),getUninstallerPath (getOS()))


module Unzip =
    
    open System.IO
    open System.IO.Compression
    open System.Reflection
    open System

    let unzipFile (zipArchivePath: string) (targetDir:string) =
        ZipFile.ExtractToDirectory(zipArchivePath, targetDir)

/// works in .fsx scripting (maybe due to FSI not closing?) but not compiled -> TO DO!
//module internal TestInstall =

//    open System
//    open System.Diagnostics
    
//    // Use this snippet to run process to check for possible errors, without much time effort.
//    let runProc filename args startDir = 
//        let procStartInfo = 
//            ProcessStartInfo(
//                RedirectStandardOutput = true,
//                RedirectStandardError = true,
//                UseShellExecute = false,
//                FileName = filename,
//                Arguments = args
//            )
//        match startDir with | Some d -> procStartInfo.WorkingDirectory <- d | _ -> ()
    
//        let outputs = System.Collections.Generic.List<string>()
//        let errors = System.Collections.Generic.List<string>()
//        let outputHandler f (_sender:obj) (args:DataReceivedEventArgs) = f args.Data
//        let p = new Process(StartInfo = procStartInfo)
//        p.OutputDataReceived.AddHandler(DataReceivedEventHandler (outputHandler outputs.Add))
//        p.ErrorDataReceived.AddHandler(DataReceivedEventHandler (outputHandler errors.Add))
//        let started = 
//            try
//                p.Start()
//            with | ex ->
//                ex.Data.Add("filename", filename)
//                reraise()
//        if not started then
//            failwithf "Failed to start process %s" filename
//        printfn "Started %s with pid %i" p.ProcessName p.Id
//        p.BeginOutputReadLine()
//        p.BeginErrorReadLine()
//        let cleanOut l = l |> Seq.filter (fun o -> String.IsNullOrEmpty o |> not)
//        let tmp = cleanOut outputs//, cleanOut errors
//        p.WaitForExit()
//        //p.Dispose()
//        tmp
    

module SideloaderCommands =

    open System.Diagnostics

    // commented due to TestInstall not running atm.
    //// returns true if correctly installed
    //let testSideloadInstall() = 
    //    let errorMsg = "The process DID NOT complete successfully."
    //    let c = TestInstall.runProc (Paths.sideloaderPath()) "-help" None
    //    Seq.contains errorMsg c |> not

    let installSwateTest() =
        System.Diagnostics.Process.Start(Paths.sideloaderPath(), ["-test"; "-manifestPath"; Paths.manifestPath()])

    let removeSwate() =
        System.Diagnostics.Process.Start(Paths.sideloaderPath(), ["-cleanup"; "-manifestPath"; Paths.manifestPath()])

open Console

let downloadMain() =

    // create config folder if not existing
    let createConfigFolder = Paths.createConfigFolder()

    printfn "Clean configs."
    let cleanFolder = 
        let files = System.IO.Directory.GetFiles (Paths.configPath())
        files |> Array.iter System.IO.File.Delete

    Console.info "Download and unzip required utilities."

    let downloadSideLoader = Download.downloadSideLoader()
    Console.info "Download Sideloader done!"
    /// Unzip downloaded sideloader zip file
    let unzip = Unzip.unzipFile (Paths.sideloaderZipPath()) (Paths.configPath())
    /// Delete downloaded sideloader zip file
    let cleanUp = System.IO.File.Delete (Paths.sideloaderZipPath())

    let downloadManifestXml = Download.downloadManifestXml()
    Console.info "Download Manifest.xml done!"

    let downloaderUninstaller = Download.downloadUninstaller()
    Console.info "Download Uninstaller done!"

    Console.ok "Finished downloading utilities."


let installMain() =
    
    Console.info "Check Sideloader status."

    // commented due to TestInstall not running atm.
    //match SideloaderCommands.testSideloadInstall() with
    //| true -> Console.ok <| "Sideloader ready to use."
    //| false -> Console.error <| "Sideloader failed to start."

    let installSwate = SideloaderCommands.installSwateTest()

    Console.ok "Swate registry done."