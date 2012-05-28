; Netduino Go Module Driver Installation Macros
; v1.0.0
; Matt Isenhower, Komodex Systems LLC
; http://komodex.com

;--------------------------------
; Temporary files

  !tempfile InstallAssemblies
  !tempfile UninstallAssemblies

  !macro CleanTempFiles
    !delfile "${InstallAssemblies}"
    !delfile "${UninstallAssemblies}"
  !macroend

;--------------------------------
; Assembly macros

  !macro AddAssembly AssemblyName SourceDirectory
    !appendfile "${InstallAssemblies}" '!insertmacro InstallAssembly "${AssemblyName}" "${SourceDirectory}"'
    !appendfile "${UninstallAssemblies}" '!insertmacro UninstallAssembly "${AssemblyName}"'
  !macroend

  !macro InstallAssembly AssemblyName SourceDirectory
    DetailPrint "Installing ${AssemblyName}..."
    
    SetOutPath "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}"
    File "${SourceDirectory}\${AssemblyName}.dll"
    File "${SourceDirectory}\${AssemblyName}.pdb"

    SetOutPath "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\be"
    File "${SourceDirectory}\be\${AssemblyName}.dll"
    File "${SourceDirectory}\be\${AssemblyName}.pdb"
    File "${SourceDirectory}\be\${AssemblyName}.pdbx"
    File "${SourceDirectory}\be\${AssemblyName}.pe"

    SetOutPath "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\le"
    File "${SourceDirectory}\le\${AssemblyName}.dll"
    File "${SourceDirectory}\le\${AssemblyName}.pdb"
    File "${SourceDirectory}\le\${AssemblyName}.pdbx"
    File "${SourceDirectory}\le\${AssemblyName}.pe"
  !macroend

  !macro UninstallAssembly AssemblyName
    DetailPrint "Removing ${AssemblyName}..."
    
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\${AssemblyName}.dll"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\${AssemblyName}.pdb"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\be\${AssemblyName}.dll"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\be\${AssemblyName}.pdb"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\be\${AssemblyName}.pdbx"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\be\${AssemblyName}.pe"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\le\${AssemblyName}.dll"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\le\${AssemblyName}.pdb"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\le\${AssemblyName}.pdbx"
    Delete "$INSTDIR\${AssemblySubdirectory}\${NETMFVersion}\le\${AssemblyName}.pe"
  !macroend

;--------------------------------
; CheckAdmin Macro (for Windows XP)

  ;On Windows Vista/7, the installer/uninstaller can request administrative privileges through UAC, but on Windows XP
  ;the user may need to run the installer as a user with administrative privileges manually.  This macro checks whether
  ;the installer is running with administrative privileges and prompts the user to run as another user if necessary.
  
  ;Two functions are provided, CheckAdmin and un.Checkadmin, which is the same as CheckAdmin but modifies the prompt
  ;to say "uninstaller" rather than "installer".

  !macro CheckAdmin un
    UserInfo::GetAccountType
    Pop $0
    StrCmp $0 "Admin" 0 +2
    return

    !if "${un}" == "un"
      StrCpy $1 "uninstall"
    !else
      StrCpy $1 "setup"
    !endif

    MessageBox MB_OK|MB_ICONSTOP \
               'This ${un}installer must be run with administrative privileges. \
                Please right-click on the $1 file and select "Run as..." \
                to select an account with administrative privileges.'
    Quit
  !macroend

  Function CheckAdmin
    !insertmacro CheckAdmin ""
  FunctionEnd

  Function un.CheckAdmin
    !insertmacro CheckAdmin "un"
  FunctionEnd
