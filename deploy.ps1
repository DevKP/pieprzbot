Write-Host "Clearing build folder.."
Remove-Item "C:\Projects\PersikSharp\Builds\*.*"
Write-Host "Renaming PersikSharp.exe to PersikSharpRelease.exe..."
Rename-Item ".\PersikSharp\bin\Release\PersikSharp.exe" "PersikSharpRelease.exe"
Write-Host "Copying files to build folder..."
xcopy /y ".\PersikSharp\bin\Release\*.*" "C:\Projects\PersikSharp\Builds\"
Write-Host "Copying configs..."
xcopy /y ".\PersikSharp\Configs\*" "C:\Projects\PersikSharp\Builds\Configs\"