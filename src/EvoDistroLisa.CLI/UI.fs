namespace EvoDistroLisa.CLI

module UI = 
    open System.IO
    open System.Windows
    open System.Threading
    open System.Windows.Media
    open System.Windows.Media.Imaging
    open EvoDistroLisa
    open EvoDistroLisa.Engine

    let applicationRef: Application ref = ref null

    let startup () = 
        let signal = new ManualResetEventSlim()
        let thread = Thread((fun () ->
            let app = new Application()
            applicationRef.Value <- app
            app.ShutdownMode <- ShutdownMode.OnExplicitShutdown
            signal.Set()
            app.Run() |> ignore
        ), IsBackground = true)
        thread.SetApartmentState(ApartmentState.STA)
        thread.Start()
        signal.Wait()

    let invoke (func: unit -> 'a) = 
        applicationRef.Value.Dispatcher.Invoke(func)

    let shutdown () = 
        invoke (fun () -> applicationRef.Value.Shutdown())
