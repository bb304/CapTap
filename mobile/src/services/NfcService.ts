/**
 * NFC hardware wrapper.
 *
 * Uses `react-native-nfc-manager` in a development / production build.
 * Expo Go and simulators report unsupported — CapTap never crashes on scan.
 */
import { Platform } from "react-native";

export type NfcAvailability = {
  supported: boolean;
  enabled: boolean;
  reason?: string;
};

export type NfcScanResult =
  | { ok: true; tagIdentifier: string }
  | { ok: false; cancelled: boolean; message: string };

type NfcManagerModule = {
  default: {
    isSupported: () => Promise<boolean>;
    isEnabled: () => Promise<boolean>;
    start: () => Promise<void>;
    cancelTechnologyRequest: () => Promise<void>;
    requestTechnology: (tech: unknown) => Promise<void>;
    getTag: () => Promise<{ id?: number[] | string } | null>;
  };
  NfcTech: { Ndef: unknown; NfcA: unknown };
};

let manager: NfcManagerModule["default"] | null = null;
let NfcTech: NfcManagerModule["NfcTech"] | null = null;
let loadError: string | null = null;

function loadNative(): boolean {
  if (manager) return true;
  if (loadError) return false;
  try {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const mod = require("react-native-nfc-manager") as NfcManagerModule;
    manager = mod.default;
    NfcTech = mod.NfcTech;
    return true;
  } catch {
    loadError =
      "NFC requires a CapTap development build. It is not available in Expo Go.";
    return false;
  }
}

function formatTagId(raw: number[] | string | undefined): string | null {
  if (!raw) return null;
  if (typeof raw === "string") {
    return raw.trim().toUpperCase();
  }
  return raw
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join(":")
    .toUpperCase();
}

export const NfcService = {
  async checkAvailability(): Promise<NfcAvailability> {
    if (Platform.OS === "web") {
      return {
        supported: false,
        enabled: false,
        reason: "NFC is not available on web.",
      };
    }

    if (!loadNative() || !manager) {
      return {
        supported: false,
        enabled: false,
        reason: loadError ?? "NFC module unavailable.",
      };
    }

    try {
      const supported = await manager.isSupported();
      if (!supported) {
        return {
          supported: false,
          enabled: false,
          reason: "This device does not support NFC.",
        };
      }

      await manager.start();
      const enabled = Platform.OS === "android" ? await manager.isEnabled() : true;
      return {
        supported: true,
        enabled,
        reason: enabled
          ? undefined
          : "NFC is turned off. Enable it in system settings.",
      };
    } catch (error) {
      return {
        supported: false,
        enabled: false,
        reason:
          error instanceof Error ? error.message : "Could not initialize NFC.",
      };
    }
  },

  async scanTag(signal?: { cancelled: boolean }): Promise<NfcScanResult> {
    const availability = await this.checkAvailability();
    if (!availability.supported || !availability.enabled || !manager || !NfcTech) {
      return {
        ok: false,
        cancelled: false,
        message: availability.reason ?? "NFC unavailable.",
      };
    }

    try {
      await manager.requestTechnology(NfcTech.Ndef ?? NfcTech.NfcA);
      if (signal?.cancelled) {
        await manager.cancelTechnologyRequest().catch(() => undefined);
        return { ok: false, cancelled: true, message: "Scan cancelled." };
      }

      const tag = await manager.getTag();
      const tagIdentifier = formatTagId(tag?.id);
      await manager.cancelTechnologyRequest().catch(() => undefined);

      if (!tagIdentifier) {
        return {
          ok: false,
          cancelled: false,
          message: "Could not read a tag identifier.",
        };
      }

      return { ok: true, tagIdentifier };
    } catch (error) {
      await manager.cancelTechnologyRequest().catch(() => undefined);
      const message = error instanceof Error ? error.message : "NFC scan failed.";
      const cancelled =
        /cancel/i.test(message) || Boolean(signal?.cancelled);
      return { ok: false, cancelled, message: cancelled ? "Scan cancelled." : message };
    }
  },

  async cancelScan(): Promise<void> {
    if (!manager) return;
    try {
      await manager.cancelTechnologyRequest();
    } catch {
      // ignore
    }
  },
};
