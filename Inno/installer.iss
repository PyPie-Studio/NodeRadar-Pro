; ============================================================================
; PyPie Studio: NodeRadar Pro - Inno Setup Script
; ============================================================================

#define MyAppName "NodeRadar Pro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PyPie Studio"
#define MyAppURL "https://github.com/PyPie-Studio/NodeRadar-Pro"
#define MyAppExeName "NodeRadar Pro.exe"
#define MySourceDir "..\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{C46A2B3D-E4D2-4E9A-8F5C-62B3F0A1D4E9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PyPie Studio\NodeRadar Pro
DefaultGroupName=PyPie Studio\NodeRadar Pro
DisableProgramGroupPage=yes
; License and branding
OutputBaseFilename=NodeRadar Pro_v{#MyAppVersion}_Setup
; SetupIconFile=..\Resources\NodeRadar Pro Icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; x64 architecture requirements
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
OutputDir=..\releases

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
