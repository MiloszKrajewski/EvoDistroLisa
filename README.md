# EvoDistroLisa
Roger Johansson's (aka Roger Alsing) EvoLisa reborn (with F# and ZeroMQ)

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
