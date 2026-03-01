; InScope Installer - Inno Setup script
; Build from project root: ISCC /DAppVersion=1.0.0 installer\InScope.iss

#ifndef AppVersion
#define AppVersion "1.0.0"
#endif

#ifndef PublishDir
#define PublishDir "..\bin\Release\net8.0-windows\win-x64\publish"
#endif

#ifndef OutputDir
#define OutputDir "..\dist"
#endif

[Setup]
AppName=InScope
AppVersion={#AppVersion}
AppVerName=InScope {#AppVersion}
AppPublisher=InScope
DefaultDirName={autopf}\InScope
DefaultGroupName=InScope
OutputBaseFilename=InScope-Setup
OutputDir={#OutputDir}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\InScope.exe

[Files]
Source: "{#PublishDir}\InScope.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PublishDir}\Content\*"; DestDir: "{app}\Content"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\InScope"; Filename: "{app}\InScope.exe"
Name: "{group}\Uninstall InScope"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\InScope.exe"; Description: "Launch InScope"; Flags: nowait postinstall skipifsilent
