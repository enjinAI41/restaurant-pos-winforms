; Inno Setup Script
; Build from repo root with installer\build-installer.ps1

#define MyAppName "Rivada Restaurant POS"
#ifndef MyAppVersion
#define MyAppVersion "1.6.0"
#endif
#ifndef PublishDir
#define PublishDir "publish\app"
#endif
#ifndef SetupOutputDir
#define SetupOutputDir "output"
#endif
#define MyAppPublisher "Rivada"
#define MyAppExeName "Rivada.Restaurant.WinForms.exe"

[Setup]
AppId={{A2D84E49-9E9A-4A22-A8EA-9A6B7F10B9D2}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Rivada Restaurant POS
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#SetupOutputDir}
OutputBaseFilename=RivadaRestaurantPOS-{#MyAppVersion}-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ek görevler:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.db,*.db-wal,*.db-shm,license*.json,license*.xml,logs\*,backups\*,*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} başlat"; Flags: nowait postinstall skipifsilent
