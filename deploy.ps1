echo "Terminating process PersikSharpRelease"
Try {
    Invoke-Expression -Command Get-Process | Where-Object { $_.Name -eq "PersikSharpRelease" } | Select-Object -First 1 | Stop-Process
}
Catch{
    echo "Process isn't running!"
}
Remove-Item "C:\Projects\PersikSharp\Builds\*.*"
Rename-Item ".\PersikSharp\bin\Release\PersikSharp.exe" "PersikSharpRelease.exe"
xcopy /y ".\PersikSharp\bin\Release\PersikSharpRelease.exe" "C:\Projects\PersikSharp\Builds\"
xcopy /y ".\PersikSharp\bin\Release\*.dll" "C:\Projects\PersikSharp\Builds\"
xcopy /y ".\PersikSharp\Configs\*" "C:\Projects\PersikSharp\Builds\Configs\"
cd  "C:\Projects\PersikSharp\Builds\"
start .\PersikSharpRelease.exe