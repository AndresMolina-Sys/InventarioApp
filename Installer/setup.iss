; Inno Setup Script for InventarioAppDesktop
; Uses self-contained .NET 8 publish output

#define MyAppName "InventarioApp"
#define MyAppVersion "1.4.0"
#define MyAppPublisher "InventarioApp"
#define MyAppURL "https://github.com/AndresMolina-Sys/InventarioApp"
#define MyAppExeName "InventarioAppDesktop.exe"

[Setup]
AppId={{4CD65DD5-DB05-4C6B-A3E4-4108EFDA8927}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\InventarioApp\{#MyAppName}
DefaultGroupName=InventarioApp
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=InventarioAppDesktopSetup-{#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
SetupIconFile=..\Resources\InventarioApp.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
DisableProgramGroupPage=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: checkedonce

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName}"; Flags: nowait postinstall skipifsilent
