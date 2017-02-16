@echo off

dotnet AtappDotNetCoreTest.dll -id 6 -c atapp_dotnetcore_test_svr.conf -p atapp_dotnetcore_test_svr.pid start -echo "hello world!"

:: dotnet AtappDotNetCoreTest.dll -id 6 -c atapp_dotnetcore_test_svr.conf run echo "say hello."