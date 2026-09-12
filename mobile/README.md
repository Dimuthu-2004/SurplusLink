# SurplusLink mobile authentication shell

This Flutter application contains only shared authentication and navigation infrastructure. It uses the ASP.NET API
endpoints `POST /api/auth/register`, `POST /api/auth/login`, and authenticated `GET /api/auth/me`.

## Packages

- `http` - reusable JSON API client.
- `flutter_secure_storage` - JWT persistence in Android encrypted storage and the Apple Keychain.
- `go_router` - authentication-aware declarative routing.

State is held by Flutter's built-in `ChangeNotifier`, so no additional state-management package is required.

## Run locally

Start the ASP.NET API on its HTTP development URL, then run Flutter with the platform-appropriate base URL:

```powershell
# Android emulator
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5170

# Windows, macOS, Linux, or web
flutter run --dart-define=API_BASE_URL=http://localhost:5170

# iOS simulator after trusting the ASP.NET development certificate
flutter run --dart-define=API_BASE_URL=https://localhost:7197
```

Android cleartext traffic is enabled only in the debug manifest for local HTTP development. Release deployments must
provide an HTTPS API URL. A physical device needs a reachable HTTPS host rather than `localhost` or `10.0.2.2`. Never put
a JWT, password, or private endpoint credential in `--dart-define`.

## Structure

```text
lib/main.dart                 composition root
lib/app.dart                  Material app and router lifetime
lib/config/app_config.dart    API base URL
lib/core/api_client.dart      reusable JSON/Bearer HTTP client
lib/core/token_storage.dart   secure JWT abstraction
lib/auth/                     models, repository, and session state
lib/routing/app_router.dart   public/protected redirects
lib/screens/                  splash, login, register, role-aware home
lib/widgets/                  shared authentication presentation widgets
```

## Verification

```powershell
flutter analyze
flutter test
```
