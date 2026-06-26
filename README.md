# PesaLens

A personal Android app for automatically tracking M-Pesa transactions by parsing SMS messages directly on your device. No data leaves your phone.

## Requirements

- .NET 10 SDK
- An Android device running API 29+ (Android 10 or newer)

## Build & Install

### 1. Clone

```bash
git clone https://github.com/KOKUMUbooker/pesa-lens.git
cd pesa-lens
```

### 2. Restore

```bash
dotnet restore
```

### 3. Build APK

```bash
cd PesaLens.App
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

The signed APK will be at:
```
PesaLens.App/bin/Release/net10.0-android/publish/com.bkokumu.pesalens-Signed.apk
```

### 4. Install on device

Transfer the APK to your Android device and open it to install. You may need to enable **Install from unknown sources** in your device settings if prompted.

## First-time Setup

PesaLens requires SMS read permission to parse M-Pesa messages. Android classifies this as a **restricted permission** and blocks it for apps installed outside the Play Store. Follow these steps to grant it:

1. Open **Settings → Apps → PesaLens → App Info**
2. Tap the **⋮ (three-dot menu)** in the top-right corner
3. Select **Allow restricted settings**
4. Go back to PesaLens and proceed through onboarding — grant the SMS permission when prompted

### Onboarding overview

| Step | What happens |
|---|---|
| Grant SMS permission | Allows real-time capture of incoming M-Pesa messages |
| Set as default SMS app *(optional)* | Required only for importing your existing M-Pesa history from your inbox |
| Restore default SMS app | Prompted immediately after history import completes |

> Ongoing transaction capture works in the background without PesaLens being your default SMS app.

## Privacy

All processing happens locally on your device. PesaLens only reads messages from the `MPESA` sender. No data is transmitted or stored externally.
