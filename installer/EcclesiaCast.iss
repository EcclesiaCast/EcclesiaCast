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
SetupIconFile=..\src\EcclesiaCast.App\app.ico
; Si el programa sigue abierto al actualizar, Windows lo cierra antes de copiar los
; archivos (en silencio no pregunta). Que no lo reabra solo: de eso se ocupa /RELANZAR.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
spanish.InstallingWebView2=Instalando el runtime de WebView2...
english.InstallingWebView2=Installing the WebView2 runtime...
spanish.WebView2DownloadFailed=No se pudo descargar el runtime de WebView2:%n%n%1%n%nLa instalación va a continuar, pero YouTube, las páginas web y la búsqueda completa de canciones en internet no van a funcionar hasta que lo instales desde:%nhttps://developer.microsoft.com/microsoft-edge/webview2/
english.WebView2DownloadFailed=The WebView2 runtime could not be downloaded:%n%n%1%n%nSetup will continue, but YouTube, web pages and the full online song search will not work until you install it from:%nhttps://developer.microsoft.com/microsoft-edge/webview2/
spanish.PerformanceGroup=Rendimiento:
english.PerformanceGroup=Performance:
spanish.LowPerformanceTask=Configurar para una PC de bajo rendimiento (recomendado en computadoras viejas o notebooks sin placa de video)
english.LowPerformanceTask=Set up for a low-performance PC (recommended for old computers or laptops without a graphics card)

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
; Sin tildar por defecto: sólo las computadoras viejas lo necesitan. Si se tilda,
; el programa arranca con el «Modo PC de bajo rendimiento» prendido (ver [Registry]).
Name: "lowperformance"; Description: "{cm:LowPerformanceTask}"; GroupDescription: "{cm:PerformanceGroup}"; Flags: unchecked

[Registry]
; La respuesta del instalador. El programa la lee al arrancar y, si todavía no hay
; un ajuste guardado, la copia a sus ajustes (HKCU, así no hace falta ser administrador).
Root: HKCU; Subkey: "Software\EcclesiaCast"; ValueType: dword; ValueName: "LowPerformance"; ValueData: 1; Tasks: lowperformance; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\EcclesiaCast"; Flags: uninsdeletekeyifempty

[Files]
; Todo el resultado de `dotnet publish` (incluye .NET y las librerías de VLC)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\EcclesiaCast.App.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\EcclesiaCast.App.exe"; Tasks: desktopicon

[Run]
; WebView2 runtime (needed for YouTube, web pages and the full online song search). The
; bootstrapper is downloaded from Microsoft during setup and only when missing,
; so the installer itself stays small and always pulls the current version.
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; \
    StatusMsg: "{cm:InstallingWebView2}"; Check: ShouldInstallWebView2; Flags: waituntilterminated
Filename: "{app}\EcclesiaCast.App.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent; Check: not ShouldRelaunch
; Una actualización desde el propio programa lo instala en silencio con /RELANZAR (ver
; EcclesiaCast.Core/Updates/UpdateInstaller.cs): el programa se cerró para dejar
; actualizar sus archivos y acá se vuelve a abrir solo, sin «skipifsilent».
; El programa lanzó este instalador ya elevado (runas), así que «postinstall» y
; «runasoriginaluser» lo abrirían como administrador, y un programa elevado no
; acepta archivos arrastrados desde el Explorador. Lo abre el Explorador, que
; siempre abre con el usuario común de la sesión.
Filename: "{win}\explorer.exe"; Parameters: """{app}\EcclesiaCast.App.exe"""; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall; Check: ShouldRelaunch

[Code]
const
  { Evergreen WebView2 runtime, per Microsoft's documented detection key. }
  WebView2ClientKey = 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';
  WebView2BootstrapperUrl = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703';

var
  WebView2Downloaded: Boolean;

function IsWebView2Installed: Boolean;
var
  Version: String;
begin
  { The EdgeUpdate keys live in the 32-bit registry view even on x64. }
  Result := (RegQueryStringValue(HKLM32, WebView2ClientKey, 'pv', Version) or
             RegQueryStringValue(HKCU, WebView2ClientKey, 'pv', Version)) and
            (Version <> '') and (Version <> '0.0.0.0');
end;

function ShouldInstallWebView2: Boolean;
begin
  Result := WebView2Downloaded;
end;

{ True when the program asked to be opened again once updated (/RELANZAR). }
function ShouldRelaunch: Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), '/RELANZAR') = 0 then
    begin
      Result := True;
      Exit;
    end;
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  Result := True;
end;

{ PrepareToInstall runs for silent installs too, unlike NextButtonClick. }
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  WebView2Downloaded := False;
  if IsWebView2Installed then
    Exit;

  try
    DownloadTemporaryFile(WebView2BootstrapperUrl, 'MicrosoftEdgeWebview2Setup.exe', '',
      @OnDownloadProgress);
    WebView2Downloaded := True;
  except
    { A missing WebView2 only disables YouTube, web pages and the song search, so a failed
      download must not abort the whole installation: report and carry on. }
    SuppressibleMsgBox(FmtMessage(CustomMessage('WebView2DownloadFailed'), [GetExceptionMessage]),
      mbInformation, MB_OK, IDOK);
  end;
end;

[UninstallDelete]
; Los datos del usuario (canciones, Biblias, temas) viven en %APPDATA%\EcclesiaCast
; y NO se borran al desinstalar, a propósito.
Type: filesandordirs; Name: "{app}\logs"
