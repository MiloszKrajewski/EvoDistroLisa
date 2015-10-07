# EvoDistroLisa
Roger Johansson's (aka Roger Alsing) EvoLisa reborn (with F# and ZMQ)

Build:
```
cd src
fake build
```

Run server:
```
cd out\build
EvoDistroLisa.CLI.exe --restart monalisa.png --agents 1 --listen 5801 --gui
```

Run client:
```
cd out\build
EvoDistroLisa.CLI.exe --connect 127.0.0.1 5801 --agents 4
```
