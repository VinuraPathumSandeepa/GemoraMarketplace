# Android development on this PC

The active app is in `C:\Users\levin\Pictures\Project\GemoraMarketplace\Mobile`.

Tools and caches are stored on D: to conserve space on C::

- Flutter: `D:\GemoraTools\flutter`
- Android SDK: `D:\GemoraTools\android-sdk`
- Java: Android Studio's runtime at `D:\AS\jbr`
- Emulator: `Gemora_Android` (Android 15, Pixel 5)
- Emulator data: `D:\GemoraTools\avd`
- Dart package cache: `D:\GemoraTools\pub-cache`
- Gradle cache: `D:\GemoraTools\gradle`

The active app's `build` and `.dart_tool` directories are junctions to D:. Keep these junctions in place when cleaning the project. The small `android/.gradle` project cache remains on C:; the main Gradle dependency cache is on D:.

Start the backend in a separate PowerShell terminal:

```powershell
cd C:\Users\levin\Pictures\Project\GemoraMarketplace\Backend\Gemora.API
dotnet run --launch-profile http
```

Then launch the mobile app:

```powershell
cd C:\Users\levin\Pictures\Project\GemoraMarketplace\Mobile
.\run-android.ps1
```

The launcher configures this terminal's tools and caches, closes the Gemora emulator while compiling, builds a debug APK, stops Gradle to release memory, and then starts the emulator and runs the prebuilt app. Emulator data is preserved. It waits up to 15 minutes for Android to finish booting and prints progress while waiting. The app connects to `http://10.0.2.2:5198`, which reaches the Windows backend from the Android emulator.

To inspect the toolchain:

```powershell
.\run-android.ps1 -CheckOnly
```

Reopen existing terminals and editors to pick up the updated user PATH. On this 8 GB PC, close unused applications before building or running the emulator. The app's Gradle heap is capped at 1 GB and its build worker count at one. Kotlin compiles inside Gradle to avoid a separate compiler process.
