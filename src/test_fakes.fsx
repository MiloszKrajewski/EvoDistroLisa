module Agent =
    let recvMany (inbox: MailboxProcessor<_>) = async { return [] }
    let send (item: 'a) (inbox: MailboxProcessor<'a>) = ()
    let start (loop: MailboxProcessor<'a> -> Async<unit>) = MailboxProcessor<'a>.Start(loop)
