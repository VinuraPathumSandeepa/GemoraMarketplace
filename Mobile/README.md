# Gemora Flutter app

Run the app from this directory with `flutter run`.

The first launch shows a four-page welcome tour. People can swipe through it, use
Continue and Back, skip it, or choose Explore Gemora or Sign in at the end. The
tour is saved locally and appears only once. The help icon in the marketplace
header opens it again at any time.

The Gemora mark is drawn in Flutter by `lib/widgets/gemora_mark.dart`. The vector
launcher source is `assets/brand/gemora_launcher.svg`. On Windows, regenerate
Android, iOS, macOS, web, and Windows icons and the native launch images with:

```powershell
./tool/generate_brand_icons.ps1
```
