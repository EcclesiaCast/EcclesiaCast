; Instalador de EcclesiaCast (Inno Setup 6)
; Se compila con:  iscc installer\EcclesiaCast.iss
; Espera la publicación self-contained en:  publish\

#define AppName "EcclesiaCast"
#define AppPublisher "EcclesiaCast"
#define AppUrl "https://github.com/EcclesiaCast/EcclesiaCast"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{8C3F1B36-4B8A-4C0E-9E4E-2A4A0D6B9E11}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=EcclesiaCast-{#AppVersion}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; La app es de 64 bits (.NET 8 self-contained x64)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\EcclesiaCast.App.exe
; SetupIconFile: agregar cuando haya un app.ico propio del proyecto.

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Todo el resultado de `dotnet publish` (incluye .NET y las librerías de VLC)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\EcclesiaCast.App.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\EcclesiaCast.App.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\EcclesiaCast.App.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Los datos del usuario (canciones, Biblias, temas) viven en %APPDATA%\EcclesiaCast
; y NO se borran al desinstalar, a propósito.
Type: filesandordirs; Name: "{app}\logs"
