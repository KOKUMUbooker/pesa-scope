# PesaScope

A personal Android app for automatically tracking M-Pesa transactions by parsing SMS messages directly on your device. No data leaves your phone.

## Requirements

- .NET 10 SDK
- An Android device running API 29+ (Android 10 or newer)

## Build & Install

### 1. Clone

```bash
git clone https://github.com/KOKUMUbooker/pesa-scope.git
cd pesa-scope
```

### 2. Restore

```bash
dotnet restore
```

### 3. Build APK

```bash
cd PesaScope.App
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

The signed APK will be at:

```
PesaScope.App/bin/Release/net10.0-android/publish/com.bkokumu.pesaScope-Signed.apk
```

### 4. Install on device

Transfer the APK to your Android device and open it to install. You may need to enable **Install from unknown sources** in your device settings if prompted.

## First-time Setup

PesaScope requires SMS read permission to parse M-Pesa messages. Android classifies this as a **restricted permission** and blocks it for apps installed outside the Play Store. Follow these steps to grant it:

1. Open **Settings → Apps → PesaScope → App Info**
2. Tap the **⋮ (three-dot menu)** in the top-right corner
3. Select **Allow restricted settings**
4. Go back to PesaScope and proceed through onboarding — grant the SMS permission when prompted

### Onboarding overview

| Step                                | What happens                                                             |
| ----------------------------------- | ------------------------------------------------------------------------ |
| Grant SMS permission                | Allows real-time capture of incoming M-Pesa messages                     |
| Set as default SMS app _(optional)_ | Required only for importing your existing M-Pesa history from your inbox |
| Restore default SMS app             | Prompted immediately after history import completes                      |

> Ongoing transaction capture works in the background without PesaScope being your default SMS app.

## Privacy

All processing happens locally on your device. PesaScope only reads messages from the `MPESA` sender. No data is transmitted or stored externally.
