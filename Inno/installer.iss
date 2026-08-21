; ; PyPie Studio: NodeRadar Pro - Inno Setup Script
; 
#define MyAppName "NodeRadar Pro"
#define MyAppPublisher "PyPie Studio"
#define MyAppURL "https://github.com/PyPie-Studio/NodeRadar-Pro"
#define MyAppExeName "NodeRadar Pro.exe"
#define MySourceDir "..\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#define MyAppExePath MySourceDir + "\" + MyAppExeName

; Dynamically read version from the compiled exe (Single Source of Truth)
#define FullAppVersion GetFileVersion(SourcePath + "\" + MyAppExePath)
#if FullAppVersion == ""
  #define MyAppVersion "1.0.0"
#else
  #if Pos(".", FullAppVersion) > 0 && RPos(".", FullAppVersion) > Pos(".", FullAppVersion)
    #define MyAppVersion Copy(FullAppVersion, 1, RPos(".", FullAppVersion) - 1)
  #else
    #define MyAppVersion FullAppVersion
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

; License and branding
OutputBaseFilename=NodeRadar Pro_v{#MyAppVersion}_Setup
; SetupIconFile=..\Resources\NodeRadar Pro Icon.ico
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
Source: "..\LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\EULA.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SECURITY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "Obfuscated,*.pdb"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
