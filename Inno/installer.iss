; ; PyPie Studio: NodeRadar Pro - Inno Setup Script
; 
#define MyAppName "NodeRadar Pro"
#define MyAppPublisher "PyPie Studio"
#define MyAppURL "https://github.com/PyPie-Studio/NodeRadar-Pro"
#define MyAppExeName "NodeRadarPro.exe"
#define MySourceDir "..\src\NodeRadarPro\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#define MyAppExePath MySourceDir + "\" + MyAppExeName

#ifndef MyAppVersion
  ; Dynamically read version from the compiled exe (Single Source of Truth)
  #define FullAppVersion GetVersionNumbersString(SourcePath + "\" + MyAppExePath)
  #if FullAppVersion == ""
    #define MyAppVersion "1.0.0"
  #else
    #if Pos(".", FullAppVersion) > 0 && RPos(".", FullAppVersion) > Pos(".", FullAppVersion)
      #define MyAppVersion Copy(FullAppVersion, 1, RPos(".", FullAppVersion) - 1)
    #else
      #define MyAppVersion FullAppVersion
    #endif
  #endif
#endif

[Setup]
; Unique AppId to track upgrades and prevent duplicate Add/Remove Program entries
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
DisableDirPage=auto
DirExistsWarning=no
PrivilegesRequired=admin

; License and branding
OutputBaseFilename=NodeRadar Pro_v{#MyAppVersion}_Setup
SetupIconFile=..\src\NodeRadarPro\Resources\NodeRadar Pro Icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

; Restart Manager to close running instances of NodeRadar Pro before updating
CloseApplications=yes
CloseApplicationsFilter={#MyAppExeName}
AppMutex=NodeRadarPro_App_Mutex_Active

; File and Installer Metadata
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=PyPie Studio
VersionInfoDescription=NodeRadar Pro Network Observability & Security Sentinel
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCopyright=© 2026 PyPie Studio

; 64-bit architecture requirements
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\releases

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
