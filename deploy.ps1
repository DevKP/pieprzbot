Write-Host "Terminating process PerchikSharpRelease"
Try {
    Invoke-Expression -Command Get-Process | Where-Object { $_.Name -eq "PerchikSharpRelease" } | Select-Object -First 1 | Stop-Process -Force
}
Catch{
    Write-Host "Process isn't running!"
}
Write-Host "Clearing build folder.."
Remove-Item "C:\Projects\PersikSharp\Builds\*.exe"
Remove-Item "C:\Projects\PersikSharp\Builds\*.dll"
Write-Host "Copying files to build folder..."
Get-ChildItem .\PerchikSharp\bin\Debug\netcoreapp2.1\ | Copy -Destination C:\Projects\PersikSharp\Builds\ -Recurse -Force
Write-Host "Copying configs..."
xcopy /y ".\PersikSharp\Configs\*" "C:\Projects\PersikSharp\Builds\Configs\"
xcopy /y ".\PersikSharp\Resources\*" "C:\Projects\PersikSharp\Builds\"
Write-Host "Starting bot!"
cd "C:\Projects\PersikSharp\Builds\" 
Start-Process -FilePath dotnet -ArgumentList 'PerchikSharp.dll'