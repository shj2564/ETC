# SoNG Browser Optimizer v1.0

Windows 11에서 Google Chrome(`chrome.exe`) 및 Microsoft Edge(`msedge.exe`) 프로세스의 메모리 상태를 확인하고 필요할 때 **작업 집합(Working Set) 정리**를 실행하는 독립 실행형 트레이 프로그램입니다.

## 주요 기능
- Chrome / Edge 별 실행 프로세스 개수, 작업 집합 합계, Private Bytes, 대략적 CPU 사용률 표시
- 수동 개별 정리 및 양쪽 브라우저 정리
- 스마트 자동 정리(기본 OFF): **시스템 RAM 사용률 85% 이상이 2회 연속 확인**되고, 브라우저가 전경에 없고, CPU 사용률이 낮고, 대상 작업 집합 합계가 512MB 이상일 때 실행. 기본 최소 간격 15분. 한 번에 한 종류의 브라우저만 정리.
- 사용자가 선택했을 때만 현재 사용자 Windows 로그인 자동 시작 등록 (`HKCU\...\Run`)
- 창을 닫거나 최소화하면 시스템 트레이에 상주하며, 트레이의 `완전히 종료`로 종료
- CMD/PowerShell 창 없는 WinForms 네이티브 EXE (Windows 기본 .NET Framework 4.8 사용)

## 사용법
1. `SoNG_Browser_Optimizer.exe`를 원하는 **고정 폴더**에 놓고 실행합니다. 관리자 권한이 필요하지 않습니다.
2. 먼저 수동 버튼으로 효과를 확인하세요.
3. 필요하면 `스마트 자동 정리 사용`에 체크하고 임계값 및 최소 간격을 조정하세요.
4. 로그인 시 자동 실행을 원하면 `Windows 로그인 시 시작`에 체크하세요.
5. 종료는 트레이 아이콘을 오른쪽 클릭 → `완전히 종료`.

## 메모리 수치 해석(중요)
- **작업 집합 합계**: 여러 프로세스의 Working Set을 단순 합산한 값으로, 프로세스 사이에 공유된 페이지가 **중복 포함**될 수 있습니다. PC의 실제 전체 RAM 사용량과 다릅니다.
- **Private Bytes**: 프로세스가 커밋한 사유 메모리의 합계입니다. 물리 RAM의 단순 점유량이 아닙니다.
- **정리 결과**는 Windows `SetProcessWorkingSetSize(-1,-1)` 호출 직전/직후의 *작업 집합 표시량 변화*입니다. 실제 사용 메모리가 영구적으로 절약됐다는 뜻이 아닙니다. 다시 메모리에 적재될 때 페이지 폴트로 잠시 느려질 수 있습니다.
- Windows 메모리가 충분할 때 굳이 반복 정리하지 않는 것이 일반적으로 유리합니다.
- 크롬 메모리 절약 모드 (`chrome://settings/performance`) 및 엣지 절전 탭 설정 (`edge://settings/system`)도 같이 활용하세요.

## 안전 및 개인정보
- 브라우저를 종료하거나 탭/쿠키/방문 기록을 삭제하지 않습니다.
- 네트워크 연결, 관리자 권한, 드라이버, 서비스 설치, 스케줄러 등록이 없습니다.
- 기본 상태에서는 **자동 정리가 꺼져 있고**, 로그인 자동 시작도 꺼져 있습니다.
- 설정 저장: `%LOCALAPPDATA%\SoNG\BrowserOptimizer\settings.ini`
- 자동 시작 선택 시만 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`의 `SoNG Browser Optimizer` 값을 변경합니다.
- 트레이 메뉴의 `완전히 종료`는 시작프로그램 등록을 해제하지 않습니다. 앱에서 체크 해제 후 종료하세요.

## 빌드 (Windows 11 / .NET Framework 4.x)
관리자 권한 없이도 Windows 제공 C# 컴파일러로 빌드 가능합니다.

```powershell
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:anycpu /optimize+ /out:SoNG_Browser_Optimizer.exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Core.dll src\Program.cs
```

## 실행파일 무변경 테스트
`SoNG_Browser_Optimizer.exe /selftest=C:\Temp\SoNG_test.txt` 명령은 프로세스와 메모리 정보를 **조회만 하고**, 지정 파일에 결과를 작성합니다. 이 테스트에서는 트림 API를 호출하지 않습니다.