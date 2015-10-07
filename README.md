# EvoDistroLisa
Roger Johansson's (aka Roger Alsing) EvoLisa reborn (with F# and ZeroMQ)

Quite some time ago Roger Johansson played with the idea of computer (re)generated art ([youtube](http://goo.gl/g6hnnm), [blog post](http://goo.gl/UY48nn)). When I started learning F# I needed a project and I decided that this can be interesting one to resurrect. 

So, this is Distributed Parallel EvoLisa reimplemented from the scrach using F#, ZeroMQ, MailboxProcessors and a little bit of Reactive Extensions. I'm just putting whatever technology I would like to try into it... 

Please note that tuning of this "hill climbing" algorithm (I think that's the name) was not my primary concern at all.

## Download

Download from [release page](https://github.com/MiloszKrajewski/EvoDistroLisa/releases)

## Build

To build it yourself:

```
release.cmd
```

Please note, executables will be in `./out/cli`

## Server
```
evo.exe --agents 1 --listen 5801 --gui --restart monalisa.png
```

## Client
```
evo.exe --connect 127.0.0.1 5801 --agents 4
```
