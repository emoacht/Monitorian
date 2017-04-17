# Monitorian

Monitorian is a Windows desktop tool to adjust the brightness of multiple monitors with ease.

![Screenshot](Images/Screenshot2.png)<br>
(DPI: 200%)

## Requirements

 * Windows 7 or newer
 * .NET Framework 4.6.2

## Download

 * Windows 10 Anniversary Update (1607) or newer:<br>
[Monitorian](https://www.microsoft.com/store/apps/9nw33j738bl0) (Windows Store)

 * Other:<br>
:floppy_disk: [Installer](https://github.com/emoacht/Monitorian/releases/download/1.2.2-Installer/MonitorianInstaller122.zip) or :floppy_disk: [Executables](https://github.com/emoacht/Monitorian/releases/download/1.2.2-Executables/Monitorian122.zip)

## Install/Uninstall

When you use only executables, please note the following:

 - The settings file will be created at `[system drive]\Users\[user name]\AppData\Local\Monitorian\`
 - The registry value will be added when you checks "Start on sign in" at `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`

## Remarks

 - An external monitor must be DDC/CI enabled.
 - The number of monitors shown at a time is currently up to 4.
 - To change the monitor name in this app, press and hold the name until it turns to be editable.

## Development

This app is a WPF app developed and tested with Surface Pro 4.

## History

Ver 1.2.2 2017-4-17

 - Fixed issue of window location

Ver 1.2.1 2017-4-3

 - Refactored

Ver 1.2.0 2017-3-29

 - Added function to show adjusted brightness

Ver 1.0.0 2017-2-22

 - Initial release

## License

 - MIT License

## Author

 - emoacht (emotom[atmark]pobox.com)
