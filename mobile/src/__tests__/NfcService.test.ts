import { NfcService } from "@/services/NfcService";

jest.mock("react-native-nfc-manager", () => {
  throw new Error("native module missing");
});

describe("NfcService", () => {
  it("reports unsupported when the native module is missing", async () => {
    const availability = await NfcService.checkAvailability();
    expect(availability.supported).toBe(false);
    expect(availability.reason).toMatch(/development build|unavailable/i);
  });

  it("returns a failed scan result on unsupported devices", async () => {
    const result = await NfcService.scanTag();
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.cancelled).toBe(false);
      expect(result.message.length).toBeGreaterThan(0);
    }
  });
});
