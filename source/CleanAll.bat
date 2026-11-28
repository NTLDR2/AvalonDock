@ECHO OFF
pushd "%~dp0"
ECHO.
ECHO.
ECHO.
ECHO This script deletes all temporary build files in their
ECHO corresponding BIN and OBJ Folder contained in the following projects
ECHO.
ECHO MLibTest
ECHO MLibTest_Components
ECHO.
ECHO MVVMTestApp
ECHO TestApp
ECHO WinFormsTestApp
ECHO.
ECHO Components\AvalonDock
ECHO Components\AvalonDock.Themes.Type1Legacy
ECHO Components\AvalonDock.Themes.Type2Hybrid
ECHO Components\AvalonDock.Themes.Type3New
ECHO.
ECHO AutomationTest\AvalonDockTest
ECHO CaliburnDockTestApp
ECHO ExperimentalTestApp
ECHO ThemeSuperTestApp
ECHO AvalonDocPanelMemoryLeaks
ECHO.
REM Ask the user if hes really sure to continue beyond this point XXXXXXXX
set /p choice=Are you sure to continue (Y/N)?
if not '%choice%'=='Y' Goto EndOfBatch
REM Script does not continue unless user types 'Y' in upper case letter
ECHO.
ECHO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
ECHO.
ECHO XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
ECHO.
ECHO Removing vs settings folder with *.sou file
ECHO.
RMDIR /S /Q .vs

ECHO Deleting BIN and OBJ Folders in MLibTest
ECHO.
RMDIR /S /Q MLibTest\MLibTest\bin
RMDIR /S /Q MLibTest\MLibTest\obj

ECHO Deleting BIN and OBJ Folders in MLibTest_Components
ECHO.
RMDIR /S /Q MLibTest\MLibTest_Components\ServiceLocator\bin
RMDIR /S /Q MLibTest\MLibTest_Components\ServiceLocator\obj

RMDIR /S /Q MLibTest\MLibTest_Components\Settings\Settings\bin
RMDIR /S /Q MLibTest\MLibTest_Components\Settings\Settings\obj

RMDIR /S /Q MLibTest\MLibTest_Components\Settings\SettingsModel\bin
RMDIR /S /Q MLibTest\MLibTest_Components\Settings\SettingsModel\obj

ECHO Deleting BIN and OBJ Folders in MVVMTestApp
ECHO.
RMDIR /S /Q MVVMTestApp\bin
RMDIR /S /Q MVVMTestApp\obj

ECHO Deleting BIN and OBJ Folders in TestApp
ECHO.
RMDIR /S /Q TestApp\bin
RMDIR /S /Q TestApp\obj

ECHO Deleting BIN and OBJ Folders in WinFormsTestApp
ECHO.
RMDIR /S /Q WinFormsTestApp\bin
RMDIR /S /Q WinFormsTestApp\obj

ECHO Deleting BIN and OBJ Folders in AvalonDock
ECHO.
RMDIR /S /Q Components\AvalonDock\bin
RMDIR /S /Q Components\AvalonDock\obj

ECHO Deleting BIN and OBJ Folders in AvalonDock.Themes.Type1Legacy
ECHO.
RMDIR /S /Q Components\AvalonDock.Themes.Type1Legacy\bin
RMDIR /S /Q Components\AvalonDock.Themes.Type1Legacy\obj

ECHO Deleting BIN and OBJ Folders in AvalonDock.Themes.Type2Hybrid
ECHO.
RMDIR /S /Q Components\AvalonDock.Themes.Type2Hybrid\bin
RMDIR /S /Q Components\AvalonDock.Themes.Type2Hybrid\obj

ECHO Deleting BIN and OBJ Folders in AvalonDock.Themes.Type3New
ECHO.
RMDIR /S /Q Components\AvalonDock.Themes.Type3New\bin
RMDIR /S /Q Components\AvalonDock.Themes.Type3New\obj

ECHO Deleting BIN and OBJ Folders in AutomationTest\AvalonDockTest
ECHO.
RMDIR /S /Q AutomationTest\AvalonDockTest\bin
RMDIR /S /Q AutomationTest\AvalonDockTest\obj

ECHO Deleting BIN and OBJ Folders in CaliburnDockTestApp
ECHO.
RMDIR /S /Q CaliburnDockTestApp\bin
RMDIR /S /Q CaliburnDockTestApp\obj

ECHO Deleting BIN and OBJ Folders in ExperimentalTestApp
ECHO.
RMDIR /S /Q ExperimentalTestApp\bin
RMDIR /S /Q ExperimentalTestApp\obj

ECHO Deleting BIN and OBJ Folders in ThemeSuperTestApp
ECHO.
RMDIR /S /Q ThemeSuperTestApp\bin
RMDIR /S /Q ThemeSuperTestApp\obj

ECHO Deleting BIN and OBJ Folders in AvalonDocPanelMemoryLeaks
ECHO.
RMDIR /S /Q AvalonDocPanelMemoryLeaks\bin
RMDIR /S /Q AvalonDocPanelMemoryLeaks\obj

PAUSE

:EndOfBatch
