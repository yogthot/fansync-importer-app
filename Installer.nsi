!include "MUI2.nsh"
!define MUI_ICON Icon.ico

Name "FanSync"
OutFile "FanSyncSetup.exe"
InstallDir "$APPDATA\FanSync"
InstallDirRegKey HKCU "Software\FanSync" ""
Unicode true

RequestExecutionLevel user

!define MUI_ABORTWARNING

!insertmacro MUI_PAGE_LICENSE "LICENSE"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
  
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!define MUI_FINISHPAGE_RUN "$INSTDIR\FanSync.exe"
!define MUI_FINISHPAGE_RUN_PARAMETERS "-setup"
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_LANGUAGE "English"

!insertmacro MUI_RESERVEFILE_LANGDLL

LangString RunAfterFinish ${LANG_ENGLISH} "Run FanSync"
LangString DESC_FanSync ${LANG_ENGLISH} "Installs the files required to run this application."
LangString DESC_StartMenuShortcuts ${LANG_ENGLISH} "Start menu shortcuts to start or uninstall the application."

!define MUI_FINISHPAGE_RUN_TEXT $(RunAfterFinish)

!getdllversion "FanSync.exe" expv_
VIProductVersion "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
VIAddVersionKey "FileVersion" "${expv_1}.${expv_2}.${expv_3}.${expv_4}"

Section "FanSync" FanSync
	SectionIn RO

	SetOutPath "$INSTDIR"
	WriteRegStr HKCU "Software\FanSync" "" "$INSTDIR"

	# for updates, stop previous version
	IfFileExists "$INSTDIR\FanSync.exe" 0 +2
		nsExec::Exec "$INSTDIR\FanSync.exe -stop"
	# fi

	File FanSync.exe
	File Microsoft.Toolkit.Uwp.Notifications.dll
	File Newtonsoft.Json.dll
	File System.ValueTuple.dll
	File COPYING
	File LICENSE

	WriteUninstaller "$INSTDIR\uninstall.exe"

	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "FanSync" '"$INSTDIR\FanSync.exe"'

	# add uninstaller to windows
	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync" "DisplayName" "FanSync"
	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync" "DisplayIcon" '"$INSTDIR\FanSync.exe"'
	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync" "UninstallString" '"$INSTDIR\uninstall.exe"'
	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
	WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync" "InstallLocation" '"$INSTDIR"'

SectionEnd

Section "Start Menu Shortcuts" StartMenuShortcuts

	CreateDirectory "$SMPROGRAMS\FanSync"
	CreateShortcut "$SMPROGRAMS\FanSync\FanSync.lnk" "$INSTDIR\FanSync.exe"
	CreateShortcut "$SMPROGRAMS\FanSync\Uninstall.lnk" "$INSTDIR\uninstall.exe"

SectionEnd

# assign descriptions to sections
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
!insertmacro MUI_DESCRIPTION_TEXT ${FanSync} $(DESC_FanSync)
!insertmacro MUI_DESCRIPTION_TEXT ${StartMenuShortcuts} $(DESC_StartMenuShortcuts)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"

	nsExec::Exec "$INSTDIR\FanSync.exe -uninstall"

	Delete "$INSTDIR\uninstall.exe"
	DeleteRegKey /ifempty HKCU "Software\FanSync"
	DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FanSync"

	Delete "$SMPROGRAMS\FanSync\FanSync.lnk"
	Delete "$SMPROGRAMS\FanSync\Uninstall.lnk"
	RMDir "$SMPROGRAMS\FanSync"

	DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "FanSync"

	Delete "$INSTDIR\FanSync.exe"
	Delete "$INSTDIR\FanSync.json"
	Delete "$INSTDIR\Microsoft.Toolkit.Uwp.Notifications.dll"
	Delete "$INSTDIR\Newtonsoft.Json.dll"
	Delete "$INSTDIR\System.ValueTuple.dll"
	Delete "$INSTDIR\COPYING"
	Delete "$INSTDIR\LICENSE"

	RMDir "$INSTDIR"

	SetAutoClose true

SectionEnd
