@echo off
setlocal enabledelayedexpansion

for /d /r %%d in (*) do (
    if exist "%%d\bin" (
        echo Deleting bin folder: %%d\bin
        rd /s /q "%%d\bin"
    )
    if exist "%%d\obj" (
        echo Deleting obj folder: %%d\obj
        rd /s /q "%%d\obj"
    )
)

echo Cleanup completed!
pause
