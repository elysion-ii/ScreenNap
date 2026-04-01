; ScreenNap Installer Script
#define MyAppName "ScreenNap"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "luxon-45"
#define MyAppURL "https://github.com/luxon-45/ScreenNap"
#define MyAppExeName "ScreenNap.exe"

[Setup]
AppId={{E4A72F8B-3D19-4C5A-9F61-B8D2E5C7A043}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=Installer
OutputBaseFilename=ScreenNap-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startmenu"; Description: "Create a Start Menu shortcut"
Name: "startup"; Description: "Start with Windows"; Flags: unchecked

[Files]
Source: "ScreenNap\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenu

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch ScreenNap"; Flags: nowait postinstall skipifsilent
