# Deployment & Packaging Guide

## Windows Desktop Notifications (Toast Packaging Requirement)

The GSAnalyzer desktop application integrates native Windows Toast Notifications via `local_notifier` (LeanFlutter desktop suite).

### Requirements for Windows Toast Popups
1. **Application Identity (AUMID) & Shortcut Policy:**
   - Windows 10/11 Toast Notification Center requires an **App User Model ID (AUMID)** to display interactive notifications with deep links.
   - Configured app name: `GSAnalyzer` (with `ShortcutPolicy.requireCreate`).
2. **Installer / Shortcut Registration:**
   - When building a distribution installer (such as MSIX or Inno Setup), ensure the Start Menu shortcut is installed with the matching `AppUserModelID`.
   - In debug/development mode, `local_notifier` automatically creates the start menu shortcut required for Windows Toast notifications.
3. **Notification Preferences Gating:**
   - All notifications (Disk, RAM, CPU, Thermal) are strictly gated on `alerts.enableDesktopNotifications` in the application settings.
   - When disabled, OS toast notifications are completely suppressed while in-app HUD banners and tab indicators remain operational.
