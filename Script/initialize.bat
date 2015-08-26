@echo off
echo.
echo Current KspPath %KspPath%
echo.
if [%1] == [] (
	echo Pass a path that points to the root KSP Folder as the first argument
	echo Usage:
	echo   initialize "D:\KSP Win 1.0"
	echo   initialize C:\KSP_Win
) else (
	if exist "%~1\KSP.exe" (
	    echo Success. KSP is found as "%~1\KSP.exe" 
	    rem only one " is not a mistake
	    setx KspPath "%~1\"
	    echo New KspPath %~1\ will be used in the future shell sessions.
	) else (
	    echo Check the path. KSP was not found at "%~1\KSP.exe"
	)
	echo.
)

