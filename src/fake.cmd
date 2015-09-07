@echo off
call %~dp0\paket.cmd restore
%~dp0\packages\FAKE\tools\FAKE.exe %*