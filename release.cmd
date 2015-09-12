@echo off
pushd %~dp0\src
call fake.cmd release
popd