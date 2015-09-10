namespace EvoDistroLisa.CLI

module UI = 
    open System.Windows
    open System.Threading

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

    let shutdown () = 
        applicationRef.Value.Shutdown()

    let invoke func = 
        applicationRef.Value.Dispatcher.Invoke(func)