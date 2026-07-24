import React, { useEffect, useRef, useState } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { Nfc } from "lucide-react-native";
import { Button, Card, Screen, SectionHeader } from "@/components/ui";
import { toUserMessage } from "@/api/errors";
import { useResolveNfcTag } from "@/hooks/useNfc";
import { NfcService } from "@/services/NfcService";
import { routes } from "@/constants/routes";
import { colors, spacing, typography } from "@/theme";

export default function NfcScanScreen() {
  const router = useRouter();
  const resolve = useResolveNfcTag();
  const cancelSignal = useRef({ cancelled: false });
  const [status, setStatus] = useState("Checking NFC…");
  const [error, setError] = useState<string | null>(null);
  const [scanning, setScanning] = useState(false);

  useEffect(() => {
    cancelSignal.current.cancelled = false;
    let active = true;

    (async () => {
      const availability = await NfcService.checkAvailability();
      if (!active) return;

      if (!availability.supported || !availability.enabled) {
        setError(
          availability.reason ??
            "NFC is not available. Use a CapTap development build on an NFC-capable phone (not Expo Go / simulator).",
        );
        setStatus("Unsupported");
        return;
      }

      setScanning(true);
      setStatus("Hold your phone near the bottle tag…");
      const result = await NfcService.scanTag(cancelSignal.current);
      if (!active) return;
      setScanning(false);

      if (!result.ok) {
        if (result.cancelled) {
          router.back();
          return;
        }
        setError(result.message);
        setStatus("Scan failed");
        return;
      }

      setStatus("Recognizing medication…");
      try {
        const resolved = await resolve.mutateAsync(result.tagIdentifier);
        if (!active) return;
        router.replace({
          pathname: routes.nfcConfirm,
          params: {
            medicationId: resolved.medicationId,
            medicationName: resolved.medicationName,
            dosageAmount: String(resolved.dosageAmount),
            dosageUnit: resolved.dosageUnit,
            form: resolved.form ?? "",
            scheduleId: resolved.scheduleId ?? "",
            scheduledTime: resolved.scheduledTime ?? "",
            scheduledDoseTime: resolved.scheduledDoseTime ?? "",
            status: resolved.status ?? "",
            alreadyLogged: resolved.alreadyLogged ? "1" : "0",
            tagIdentifier: resolved.tagIdentifier,
          },
        });
      } catch (err) {
        if (!active) return;
        const message = toUserMessage(err);
        setError(
          /not found/i.test(message)
            ? "This tag isn't linked to your medications. Assign it from a medication's details screen, or try another sticker."
            : message,
        );
        setStatus("Could not resolve tag");
      }
    })();

    return () => {
      active = false;
      cancelSignal.current.cancelled = true;
      void NfcService.cancelScan();
    };
    // Start one scan session per mount.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <Screen>
      <SectionHeader title="Scan bottle" subtitle="Tap. Confirm. Peace of mind." />
      <Card style={styles.hero}>
        <Nfc color={colors.primaryDark} size={48} />
        <Text style={styles.status} maxFontSizeMultiplier={1.4}>
          {status}
        </Text>
        {error ? (
          <Text style={styles.error} maxFontSizeMultiplier={1.4}>
            {error}
          </Text>
        ) : null}
        <View style={styles.actions}>
          {scanning ? (
            <Button
              label="Cancel scan"
              variant="secondary"
              onPress={() => {
                cancelSignal.current.cancelled = true;
                void NfcService.cancelScan();
                router.back();
              }}
            />
          ) : (
            <Button label="Back" variant="ghost" onPress={() => router.back()} />
          )}
        </View>
      </Card>
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: { alignItems: "center", gap: spacing.md, paddingVertical: spacing.xl },
  status: { ...typography.subtitle, textAlign: "center" },
  error: { ...typography.bodyMuted, color: colors.danger, textAlign: "center" },
  actions: { width: "100%", marginTop: spacing.md },
});
