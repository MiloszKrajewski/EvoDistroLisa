namespace EvoDistroLisa

module Pickler =
    open LZ4
    open Nessos.FsPickler

    let pickler = FsPickler.CreateBinarySerializer(true)

    let save (message: 'a) = 
        LZ4Codec.Wrap(pickler.Pickle(message))

    let load<'a> (message: byte[]) =
        pickler.UnPickle<'a>(LZ4Codec.Unwrap(message))
