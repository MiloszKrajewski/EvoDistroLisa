@echo off
set this=%~dp0
set target=%cd%
set platform=%1
set filename=%2
copy /y /d %this%\%platform%\%filename% %target%\%filename%
