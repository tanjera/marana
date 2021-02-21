cd marana
dotnet clean
dotnet build -c Release
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64

cd bin\Release\net5.0\win-x64\
del /q *
move publish marana
tar -c -f windows.zip marana
move windows.zip ..\..\..

cd ..\linux-x64
del /q *
move publish marana
tar -c -f linux.zip marana
move linux.zip ..\..\..

cd ..\..\..\..\..